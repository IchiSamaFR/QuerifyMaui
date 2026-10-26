using CommunityToolkit.Maui;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Querify.UI.Views.Controls.Menus;

/// <summary>
/// MenuFlyoutSubItem alimenté par une collection de chaînes (MenuFlyoutSubItem n'a pas d'ItemsSource).
/// Chaque chaîne devient une entrée dont la commande reçoit la chaîne en paramètre.
/// </summary>
public partial class MenuFlyoutSubItemExtended : MenuFlyoutSubItem
{
    /// <summary>Éléments affichés dans le sous-menu.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnItemsSourceChanged))]
    public partial IEnumerable<string>? ItemsSource { get; set; }

    /// <summary>Commande exécutée au clic sur un élément ; paramètre : l'élément.</summary>
    [BindableProperty]
    public partial ICommand? ItemCommand { get; set; }

    /// <summary>Commande du pied de menu (ex. "Effacer la liste"). Pas de pied si null.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnFooterCommandChanged))]
    public partial ICommand? FooterCommand { get; set; }

    /// <summary>Texte du pied de menu.</summary>
    [BindableProperty]
    public partial string FooterText { get; set; } = "Effacer la liste";

    /// <summary>Texte affiché quand la collection est vide.</summary>
    [BindableProperty]
    public partial string EmptyText { get; set; } = "Aucun élément";

    public MenuFlyoutSubItemExtended()
    {
        InitializeComponent();
        Rebuild();
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var menu = (MenuFlyoutSubItemExtended)bindable;

        if (oldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= menu.OnCollectionChanged;
        if (newValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += menu.OnCollectionChanged;

        menu.Rebuild();
    }

    private static void OnFooterCommandChanged(BindableObject bindable, object? oldValue, object? newValue)
        => ((MenuFlyoutSubItemExtended)bindable).Rebuild();

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    // MenuFlyoutSubItem n'a pas de visibilite par entree : la liste est recomposee a chaque changement.
    private void Rebuild()
    {
        Clear();

        var items = ItemsSource?.ToList() ?? [];
        if (items.Count == 0)
        {
            Add(EmptyItem);
            return;
        }

        for (var i = 0; i < items.Count; i++)
            Add(CreateItem(i, items[i]));

        if (FooterCommand is not null)
        {
            Add(FooterSeparator);
            Add(FooterItem);
        }
    }

    private MenuFlyoutItem CreateItem(int index, string value)
    {
        var item = new MenuFlyoutItem { Text = $"{index + 1}  {value}", CommandParameter = value };
        item.SetBinding(CommandProperty, new Binding(nameof(ItemCommand), source: this));
        return item;
    }
}
