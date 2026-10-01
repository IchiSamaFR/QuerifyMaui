using Microsoft.Maui.Handlers;

namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// Handler du Picker qui force le fond de la fenêtre de sélection, indépendamment du
/// thème applicatif (neutralisé au runtime par UraniumUI Material / le mode jour forcé).
/// Implémentations par plateforme : ThemedPickerHandler.Android.cs / ThemedPickerHandler.iOS.cs /
/// ThemedPickerHandler.Windows.cs.
/// </summary>
public partial class ThemedPickerHandler : PickerHandler
{
    // Fond de la fenêtre du Picker, aligné sur le thème sombre de l'app.
    private static string? _dialogBackgroundHex;
    private static string DialogBackgroundHex
    {
        get
        {
            if (_dialogBackgroundHex == null)
            {
                var resource = Application.Current?.Resources.TryGetValue("DialogBackgroundColor", out var value) == true
                    ? (Color)value : null;
                _dialogBackgroundHex = resource?.ToHex() ?? null;
            }

            return _dialogBackgroundHex ?? "#2C2C2C";
        }
    }

    // Couleur d'action (boutons du dialog), alignée sur la couleur Primary de l'app.
    private static string? _primaryHex;
    private static string PrimaryHex
    {
        get
        {
            if (_primaryHex == null)
            {
                var resource = Application.Current?.Resources.TryGetValue("Primary", out var value) == true
                    ? (Color)value : null;
                _primaryHex = resource?.ToHex() ?? null;
            }

            return _primaryHex ?? "#03A9F4";
        }
    }
}
