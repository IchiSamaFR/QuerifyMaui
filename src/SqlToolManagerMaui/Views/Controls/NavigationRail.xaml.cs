using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls;

/// <summary>
/// Volet de navigation lateral (style Windows 11 NavigationView).
/// Les items sont declares depuis l'exterieur (AppShell.xaml) via <see cref="Items"/>
/// et <see cref="FooterItems"/> ; le rail leur propage automatiquement
/// <see cref="SelectedRoute"/>, <see cref="IsLabelVisible"/> et <see cref="ItemCommand"/>.
/// </summary>
[ContentProperty(nameof(Items))]
public partial class NavigationRail : ContentView
{
    public static readonly BindableProperty SelectedRouteProperty =
        BindableProperty.Create(nameof(SelectedRoute), typeof(string), typeof(NavigationRail), string.Empty);

    public static readonly BindableProperty IsLabelVisibleProperty =
        BindableProperty.Create(nameof(IsLabelVisible), typeof(bool), typeof(NavigationRail), true);

    public static readonly BindableProperty ItemCommandProperty =
        BindableProperty.Create(nameof(ItemCommand), typeof(ICommand), typeof(NavigationRail), null);

    public static readonly BindableProperty ToggleCommandProperty =
        BindableProperty.Create(nameof(ToggleCommand), typeof(ICommand), typeof(NavigationRail), null);

    /// <summary>Route actuellement affichee (pilote l'etat selectionne des items).</summary>
    public string SelectedRoute
    {
        get => (string)GetValue(SelectedRouteProperty);
        set => SetValue(SelectedRouteProperty, value);
    }

    /// <summary>Affiche ou masque les libelles (mode etendu / compact).</summary>
    public bool IsLabelVisible
    {
        get => (bool)GetValue(IsLabelVisibleProperty);
        set => SetValue(IsLabelVisibleProperty, value);
    }

    /// <summary>Commande de navigation appelee par les items, avec la route en parametre.</summary>
    public ICommand? ItemCommand
    {
        get => (ICommand?)GetValue(ItemCommandProperty);
        set => SetValue(ItemCommandProperty, value);
    }

    /// <summary>Commande du bouton hamburger.</summary>
    public ICommand? ToggleCommand
    {
        get => (ICommand?)GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

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
