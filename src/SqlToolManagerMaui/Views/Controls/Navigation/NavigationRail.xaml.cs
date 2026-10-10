using CommunityToolkit.Maui;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Navigation;

/// <summary>
/// Volet de navigation lateral (style Windows 11 NavigationView).
/// Les items sont declares depuis l'exterieur (AppShell.xaml) via <see cref="Items"/>
/// et <see cref="FooterItems"/> ; le rail leur propage automatiquement
/// <see cref="SelectedRoute"/>, <see cref="IsLabelVisible"/> et <see cref="ItemCommand"/>.
/// </summary>
[ContentProperty(nameof(Items))]
public partial class NavigationRail : ContentView
{
    /// <summary>Route actuellement affichee (pilote l'etat selectionne des items).</summary>
    [BindableProperty]
    public partial string SelectedRoute { get; set; } = string.Empty;

    /// <summary>Affiche ou masque les libelles (mode etendu / compact).</summary>
    [BindableProperty]
    public partial bool IsLabelVisible { get; set; } = true;

    /// <summary>Commande de navigation appelee par les items, avec la route en parametre.</summary>
    [BindableProperty]
    public partial ICommand? ItemCommand { get; set; }

    /// <summary>Commande du bouton hamburger.</summary>
    [BindableProperty]
    public partial ICommand? ToggleCommand { get; set; }

    /// <summary>Items principaux du menu (contenu par defaut du controle).</summary>
    public IList<NavigationRailItem> Items { get; } = new ObservableCollection<NavigationRailItem>();

    /// <summary>Items ancres en bas du rail (parametres, compte, ...).</summary>
    public IList<NavigationRailItem> FooterItems { get; } = new ObservableCollection<NavigationRailItem>();

    public NavigationRail()
    {
        InitializeComponent();

        ((ObservableCollection<NavigationRailItem>)Items).CollectionChanged +=
            (_, e) => Sync(e, ItemsHost);

        ((ObservableCollection<NavigationRailItem>)FooterItems).CollectionChanged +=
            (_, e) => Sync(e, FooterHost);
    }

    private void Sync(NotifyCollectionChangedEventArgs e, Layout host)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            host.Clear();
            return;
        }

        if (e.OldItems is not null)
        {
            foreach (NavigationRailItem item in e.OldItems)
                host.Remove(item);
        }

        if (e.NewItems is null)
            return;

        var index = e.NewStartingIndex < 0 ? host.Count : e.NewStartingIndex;
        foreach (NavigationRailItem item in e.NewItems)
        {
            Attach(item);
            host.Insert(Math.Clamp(index++, 0, host.Count), item);
        }
    }

    /// <summary>Branche l'item sur l'etat du rail : selection, libelle et commande.</summary>
    private void Attach(NavigationRailItem item)
    {
        item.SetBinding(NavigationRailItem.SelectedRouteProperty,
            new Binding(nameof(SelectedRoute), source: this));

        item.SetBinding(NavigationRailItem.IsLabelVisibleProperty,
            new Binding(nameof(IsLabelVisible), source: this));

        if (item.Command is null)
        {
            item.SetBinding(NavigationRailItem.CommandProperty,
                new Binding(nameof(ItemCommand), source: this));
        }
    }
}
