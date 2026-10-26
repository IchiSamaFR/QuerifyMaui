using Microsoft.UI.Input;
using System.Reflection;
using WinUIElement = Microsoft.UI.Xaml.UIElement;

namespace Querify.UI.Extensions;

/// <summary>Curseur de souris d'une vue native WinUI.</summary>
public static class CursorExtensions
{
    // ProtectedCursor est protege dans WinUI : on passe par la reflexion.
    private static readonly PropertyInfo? ProtectedCursorProperty =
        typeof(WinUIElement).GetProperty("ProtectedCursor", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void SetCursor(this WinUIElement view, InputSystemCursorShape shape)
        => ProtectedCursorProperty?.SetValue(view, InputSystemCursor.Create(shape));
}
