using CommunityToolkit.Maui;
using System.Collections;
using System.Windows.Input;

namespace Querify.UI.Views.Controls.Tabs;

/// <summary>
/// Controle a onglets facon WPF / Windows 11 : barre d'onglets en haut, contenu de l'onglet selectionne en dessous.
/// Onglets declares en enfants (<see cref="Items"/>) ou generes depuis <see cref="ItemsSource"/> via <see cref="ItemTemplate"/>.
/// Contenu affiche : <see cref="TabBarItem.Content"/>, sinon <see cref="ContentSelector"/>, sinon <see cref="DefaultContent"/>.
/// </summary>
public partial class TabBar : ContentView
{
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial IEnumerable ItemsSource { get; set; }

    /// <summary>Element de <see cref="ItemsSource"/>, ou le <see cref="TabBarItem"/> en mode <see cref="Items"/>.</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedItemChanged), CoerceValueMethodName = nameof(CoerceSelectedItem))]
    public partial object SelectedItem { get; set; }

    /// <summary>Onglets reordonnables par glisser-deposer (<see cref="ItemsSource"/> doit etre une collection observable).</summary>
    [BindableProperty]
    public partial bool CanReorderItems { get; set; }

    [BindableProperty]
    public partial string TitleMemberPath { get; set; } = string.Empty;

    [BindableProperty]
    public partial string ToolTipMemberPath { get; set; } = string.Empty;

    #region ---- Template properties ----
    /// <summary>Doit produire un <see cref="TabBarItem"/> ; son BindingContext est l'element de la source.</summary>
    [BindableProperty]
    public partial DataTemplate ItemTemplate { get; set; }

    /// <summary>
    /// Choisit la vue de l'element selectionne (BindingContext = <see cref="SelectedItem"/>) ; les vues appartiennent
    /// au selecteur et sont reutilisees d'un onglet a l'autre.
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectedItemChanged))]
    public partial ViewSelector? ContentSelector { get; set; }

    [BindableProperty]
    public partial View TrailingContent { get; set; }

    /// <summary>Contenu partage, affiche quand l'onglet selectionne n'en fournit pas.</summary>
    [BindableProperty]
    public partial View DefaultContent { get; set; }

    [BindableProperty]
    public partial View SelectedContent { get; private set; }

    [BindableProperty]
    public partial bool IsClosable { get; set; } = true;

    /// <summary>
    /// Fermeture geree par le consommateur (parametre : l'element de l'onglet), qui retire alors lui-meme l'element.
    /// Sans commande, la TabBar retire l'element de <see cref="ItemsSource"/> (qui doit etre une <see cref="IList"/>).
    /// </summary>
    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    /// <summary>Vues deja affichees par le selecteur, pour leur retirer l'element une fois masquees.</summary>
    private readonly HashSet<View> _selectedContents = new();

    /// <summary>Commande transmise aux en-tetes : aiguille vers <see cref="CloseCommand"/> ou la fermeture interne.</summary>
    public ICommand CloseTabCommand { get; }
    #endregion

    public TabBar()
    {
        // Avant InitializeComponent : les bindings des en-tetes doivent trouver la commande deja creee.
        CloseTabCommand = new Command<object>(OnCloseTabRequested);

        InitializeComponent();
    }

    /// <summary>
    /// La liste native perd sa selection quand un onglet est deplace (ou au Ctrl+clic) : tant que l'onglet
    /// selectionne existe encore, il le reste, et la liste est resynchronisee apres coup.
    /// </summary>
    private static object CoerceSelectedItem(BindableObject bindable, object value)
    {
        var tabBar = (TabBar)bindable;
        if (value is not null || tabBar.SelectedItem is not { } current || tabBar.ItemsSource?.Cast<object>().Contains(current) != true)
            return value!;

        tabBar.Dispatcher.Dispatch(() => tabBar.TabsList.SelectedItem = tabBar.SelectedItem);
        return current;
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tabBar = (TabBar)bindable;
        tabBar.SelectedContent = tabBar.GetSelectedContent() ?? tabBar.DefaultContent;
    }

    private View? GetSelectedContent()
    {
        var content = SelectedItem is null ? null : ContentSelector?.SelectView(SelectedItem, this);
        if (content is not null)
            _selectedContents.Add(content);

        // Une vue masquee lache son element : ce qu'elle alimente sait qu'elle n'est plus affichee.
        foreach (var view in _selectedContents.Where(v => !ReferenceEquals(v, content)))
            view.BindingContext = null;

        if (content is not null)
            content.BindingContext = SelectedItem;

        return content;
    }

    private void OnCloseTabRequested(object? tab)
    {
        if (tab is null || !IsClosable)
            return;

        if (CloseCommand is not null)
        {
            if (CloseCommand.CanExecute(tab))
                CloseCommand.Execute(tab);
            return;
        }

        CloseTab(tab);
    }

    private void CloseTab(object? tab)
    {
        if (tab is null || !IsClosable)
            return;

        if (ItemsSource is not IList list)
            throw new NotSupportedException(
                $"ItemsSource must be an IList, not {ItemsSource?.GetType().Name ?? "null"}.");

        var index = list.IndexOf(tab);
        if (index < 0)
            return;

        var wasSelected = ReferenceEquals(SelectedItem, tab);

        list.Remove(tab);

        // Si l'onglet fermé était sélectionné, sélectionner le voisin (ou rien s'il ne reste aucun onglet).
        if (wasSelected)
        {
            SelectedItem = list.Count == 0
                ? null!
                : list[Math.Min(index, list.Count - 1)]!;
        }
    }
}
