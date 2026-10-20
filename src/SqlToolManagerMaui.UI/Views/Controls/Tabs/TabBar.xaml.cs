using CommunityToolkit.Maui;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Views.Controls.Tabs;

/// <summary>
/// Controle a onglets facon WPF / Windows 11 : barre d'onglets en haut, contenu de l'onglet selectionne en dessous.
/// Onglets declares en enfants (<see cref="Items"/>) ou generes depuis <see cref="ItemsSource"/> via <see cref="ItemTemplate"/>.
/// Contenu affiche : <see cref="TabBarItem.Content"/>, sinon <see cref="ContentTemplate"/>, sinon <see cref="DefaultContent"/>.
/// </summary>
public partial class TabBar : ContentView
{
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial IEnumerable ItemsSource { get; set; }

    /// <summary>Element de <see cref="ItemsSource"/>, ou le <see cref="TabBarItem"/> en mode <see cref="Items"/>.</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedItemChanged))]
    public partial object SelectedItem { get; set; }

    [BindableProperty]
    public partial string TitleMemberPath { get; set; } = string.Empty;

    [BindableProperty]
    public partial string ToolTipMemberPath { get; set; } = string.Empty;

    #region ---- Template properties ----
    /// <summary>Doit produire un <see cref="TabBarItem"/> ; son BindingContext est l'element de la source.</summary>
    [BindableProperty]
    public partial DataTemplate ItemTemplate { get; set; }

    /// <summary>
    /// Contenu genere pour l'element selectionne (BindingContext = <see cref="SelectedItem"/>). Accepte un
    /// <see cref="DataTemplateSelector"/> ; une vue est creee une fois par gabarit puis reutilisee d'un onglet a l'autre.
    /// </summary>
    [BindableProperty]
    public partial DataTemplate ContentTemplate { get; set; }

    [BindableProperty]
    public partial View TrailingContent { get; set; }

    /// <summary>Contenu partage, affiche quand l'onglet selectionne n'en fournit pas.</summary>
    [BindableProperty]
    public partial View DefaultContent { get; set; }

    [BindableProperty]
    public partial View SelectedContent { get; private set; }

    [BindableProperty]
    public partial bool IsClosable { get; set; } = true;

    private readonly Dictionary<DataTemplate, View> _templatedContents = new();

    #endregion

    public TabBar()
    {
        InitializeComponent();
    }

    private static void OnSelectedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var tabBar = (TabBar)bindable;
        tabBar.SelectedContent = tabBar.GetTemplatedContent() ?? tabBar.DefaultContent;
    }

    private View? GetTemplatedContent()
    {
        var template = ContentTemplate is null || SelectedItem is null
            ? null
            : ContentTemplate is DataTemplateSelector selector ? selector.SelectTemplate(SelectedItem, this) : ContentTemplate;

        // Recreer la vue a chaque changement d'onglet reconstruirait des controles lourds (editeur natif).
        View? content = null;
        if (template is not null && !_templatedContents.TryGetValue(template, out content) && template.CreateContent() is View created)
        {
            content = created;
            _templatedContents[template] = content;
        }

        // Une vue masquee lache son element : ce qu'elle alimente sait qu'elle n'est plus affichee.
        foreach (var view in _templatedContents.Values.Where(v => !ReferenceEquals(v, content)))
            view.BindingContext = null;

        if (content is not null)
            content.BindingContext = SelectedItem;

        return content;
    }
}
