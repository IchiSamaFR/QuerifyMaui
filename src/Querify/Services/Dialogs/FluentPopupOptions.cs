using CommunityToolkit.Maui;
using Microsoft.Maui.Controls.Shapes;

namespace Querify.Services.Dialogs;

/// <summary>Habillage des popups facon WPF UI : coins 8 px, trait SurfaceStroke, voile SmokeFill.</summary>
public static class FluentPopupOptions
{
    public static PopupOptions Create() => new()
    {
        CanBeDismissedByTappingOutsideOfPopup = false,
        PageOverlayColor = Resource("SmokeFill", Color.FromArgb("#4D000000")),
        Shape = new RoundRectangle
        {
            CornerRadius = 8,
            Stroke = Resource("SurfaceStrokeColorDefault", Color.FromArgb("#66757575")),
            StrokeThickness = 1,
        },
    };

    private static Color Resource(string key, Color fallback)
        => Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
}
