using CommunityToolkit.Maui;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Views.Controls.Menus;


/// <summary>
/// MenuFlyoutSubItem alimenté par une collection de chaînes (MenuFlyoutSubItem n'a pas d'ItemsSource).
/// Chaque chaîne devient une entrée dont la commande reçoit la chaîne en paramètre.
/// </summary>
public partial class MenuFlyoutSubItemExtended : MenuFlyoutSubItem
{
    /// <summary>Éléments affichés dans le sous-menu.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnMenuChanged))]
    public partial IEnumerable<string>? ItemsSource { get; set; }

    /// <summary>Commande exécutée au clic sur un élément ; paramètre : l'élément.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnMenuChanged))]
    public partial ICommand? ItemCommand { get; set; }

    /// <summary>Commande du pied de menu (ex. "Effacer la liste"). Pas de pied si null.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnMenuChanged))]
    public partial ICommand? FooterCommand { get; set; }

    /// <summary>Texte du pied de menu.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnMenuChanged))]
    public partial string FooterText { get; set; } = "Effacer la liste";

    /// <summary>Texte affiché quand la collection est vide.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnMenuChanged))]
    public partial string EmptyText { get; set; } = "Aucun élément";

    public MenuFlyoutSubItemExtended() => Rebuild();

    private static void OnMenuChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var menu = (MenuFlyoutSubItemExtended)bindable;

        if (oldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= menu.OnCollectionChanged;
        if (newValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += menu.OnCollectionChanged;

        menu.Rebuild();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        Clear();

        var items = ItemsSource?.ToList() ?? [];
        if (items.Count == 0)
        {
            Add(new MenuFlyoutItem { Text = EmptyText, IsEnabled = false });
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            Add(new MenuFlyoutItem
            {
                Text = $"{i + 1}  {items[i]}",
                Command = ItemCommand,
                CommandParameter = items[i],
            });
        }

        if (FooterCommand is not null)
        {
            Add(new MenuFlyoutSeparator());
            Add(new MenuFlyoutItem { Text = FooterText, Command = FooterCommand });
        }
    }
}