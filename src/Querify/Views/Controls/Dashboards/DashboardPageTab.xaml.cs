using CommunityToolkit.Maui;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Windows.Input;

namespace Querify.Views.Controls.Dashboards;

/// <summary>Onglet d'une page dans le concepteur : clic = ouverture ; la page editee se renomme sur place, ses options portent la cle et la suppression.</summary>
public partial class DashboardPageTab : ContentView
{
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial string? Title { get; set; }

    /// <summary>Page affichee dans le concepteur.</summary>
    [BindableProperty]
    public partial bool IsCurrent { get; set; }

    [BindableProperty]
    public partial ICommand? OpenCommand { get; set; }

    /// <summary>Options de la page : cle et libelle.</summary>
    [BindableProperty]
    public partial ICommand? EditCommand { get; set; }

    [BindableProperty]
    public partial ICommand? DeleteCommand { get; set; }

    /// <summary>Transmis aux commandes de l'onglet (la page).</summary>
    [BindableProperty]
    public partial object? CommandParameter { get; set; }

    public DashboardPageTab()
    {
        InitializeComponent();
    }

    // WinUI n'ouvre un ContextFlyout qu'au clic droit : on l'affiche nous-memes au clic gauche.
    private void OnOptionsClicked(object? sender, EventArgs e)
    {
        if (sender is Button { Handler.PlatformView: Microsoft.UI.Xaml.FrameworkElement { ContextFlyout: { } flyout } view })
            flyout.ShowAt(view, new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
    }
}
