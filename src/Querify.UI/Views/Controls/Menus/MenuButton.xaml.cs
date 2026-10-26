using CommunityToolkit.Maui;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Querify.UI.Views.Controls.Menus;

/// <summary>Bouton icone qui ouvre, sous lui, le menu pose dans son <c>FlyoutBase.ContextFlyout</c>.</summary>
public partial class MenuButton : ContentView
{
    [BindableProperty]
    public partial ImageSource? ImageSource { get; set; }

    public MenuButton()
    {
        InitializeComponent();
    }

    // WinUI n'ouvre un ContextFlyout qu'au clic droit : on l'affiche nous-memes au clic gauche.
    private void OnClicked(object? sender, EventArgs e)
    {
        if (Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement { ContextFlyout: { } flyout } view)
            flyout.ShowAt(view, new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
    }
}
