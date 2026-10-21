using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Application = Microsoft.Maui.Controls.Application;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
using WinColor = Windows.UI.Color;
using WinThickness = Microsoft.UI.Xaml.Thickness;

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

    protected override void ConnectHandler(ComboBox platformView)
    {
        base.ConnectHandler(platformView);
        ApplyTheme(platformView);
    }

    /// <summary>
    /// La "fenêtre" du Picker Windows est le popup du ComboBox. Il n'est pas dans l'arbre visuel
    /// de la page mais dans le template du contrôle : surcharger les ressources WinUI portées par
    /// le ComboBox suffit donc à l'habiller, indépendamment du thème système / applicatif.
    /// </summary>
    private void ApplyTheme(ComboBox comboBox)
    {
        // Base sombre : évite que les ressources non surchargées (scrollbar, ombres…) repassent en clair.
        comboBox.RequestedTheme = ElementTheme.Dark;

        var background = BrushFromHex(DialogBackgroundHex);
        var accent = BrushFromHex(PrimaryHex);
        var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        var foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            VirtualView?.TextColor?.ToWindowsColor() ?? Microsoft.UI.Colors.White);

        var resources = comboBox.Resources;

        // ── Popup de sélection ────────────────────────────────────────────────
        resources["ComboBoxDropDownBackground"] = background;
        resources["ComboBoxDropDownBackgroundPointerOver"] = background;
        resources["ComboBoxDropDownBackgroundPointerPressed"] = background;
        resources["ComboBoxDropDownBorderBrush"] = transparent;
        resources["ComboBoxDropDownForeground"] = foreground;
        resources["ComboBoxDropDownGlyphForeground"] = foreground;

        // ── Éléments de la liste ──────────────────────────────────────────────
        resources["ComboBoxItemForeground"] = foreground;
        resources["ComboBoxItemForegroundPointerOver"] = foreground;
        resources["ComboBoxItemForegroundPressed"] = foreground;
        resources["ComboBoxItemForegroundSelected"] = foreground;
        resources["ComboBoxItemForegroundSelectedPointerOver"] = foreground;
        resources["ComboBoxItemForegroundSelectedPressed"] = foreground;
        resources["ComboBoxItemBackground"] = transparent;
        resources["ComboBoxItemBackgroundPointerOver"] = WithOpacity(accent, 0.20);
        resources["ComboBoxItemBackgroundPressed"] = WithOpacity(accent, 0.30);
        resources["ComboBoxItemBackgroundSelected"] = WithOpacity(accent, 0.35);
        resources["ComboBoxItemBackgroundSelectedPointerOver"] = WithOpacity(accent, 0.45);
        resources["ComboBoxItemBackgroundSelectedPressed"] = WithOpacity(accent, 0.45);
        // Barre d'accent à gauche de l'élément sélectionné.
        resources["ComboBoxItemPillFillBrush"] = accent;

        // ── Champ fermé : transparent, InputField dessine déjà sa bordure ──────
        resources["ComboBoxBackground"] = transparent;
        resources["ComboBoxBackgroundPointerOver"] = transparent;
        resources["ComboBoxBackgroundPressed"] = transparent;
        resources["ComboBoxBackgroundFocused"] = transparent;
        resources["ComboBoxBackgroundUnfocused"] = transparent;
        resources["ComboBoxBorderBrush"] = transparent;
        resources["ComboBoxBorderBrushPointerOver"] = transparent;
        resources["ComboBoxBorderBrushPressed"] = transparent;
        resources["ComboBoxBorderBrushFocused"] = transparent;
        resources["ComboBoxForeground"] = foreground;
        resources["ComboBoxForegroundPointerOver"] = foreground;
        resources["ComboBoxForegroundPressed"] = foreground;
        resources["ComboBoxForegroundFocused"] = foreground;
        resources["ComboBoxDropDownGlyphForegroundPointerOver"] = accent;
        resources["ComboBoxDropDownGlyphForegroundPressed"] = accent;

        comboBox.Background = transparent;
        comboBox.BorderThickness = new WinThickness(0);
        comboBox.Padding = new WinThickness(0);
        comboBox.MinHeight = 0;

        // Hauteur et padding du template WinUI (min. 32) : sans ca, le texte est rogne dans un champ de 24.
        resources["ComboBoxMinHeight"] = 0d;
        resources["ComboBoxPadding"] = new WinThickness(0);
        comboBox.Padding = new WinThickness(0);
        comboBox.Margin = new WinThickness(0);
        comboBox.BorderThickness = new WinThickness(0);
        comboBox.VerticalAlignment = VerticalAlignment.Stretch;
        comboBox.VerticalContentAlignment = VerticalAlignment.Stretch;
        comboBox.Loaded += OnComboBoxLoaded;
    }

    /// <summary>Le ContentPresenter du template garde une marge verticale propre : on la retire une fois le template applique.</summary>
    private static void OnComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        var comboBox = (ComboBox)sender;
        comboBox.Loaded -= OnComboBoxLoaded;

        if (FindByName(comboBox, "ContentPresenter") is FrameworkElement presenter)
        {
            presenter.Margin = new WinThickness(0);
            presenter.VerticalAlignment = VerticalAlignment.Stretch;
        }
    }

    private static FrameworkElement? FindByName(DependencyObject root, string name)
    {
        for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement { } element && element.Name == name)
                return element;

            if (FindByName(child, name) is { } found)
                return found;
        }

        return null;
    }

    private static Microsoft.UI.Xaml.Media.SolidColorBrush WithOpacity(Microsoft.UI.Xaml.Media.SolidColorBrush brush, double opacity)
        => new(brush.Color) { Opacity = opacity };

    private static Microsoft.UI.Xaml.Media.SolidColorBrush BrushFromHex(string hex) => new(ColorFromHex(hex));

    /// <summary>Accepte #RGB, #RRGGBB et #AARRGGBB (format rendu par <c>Color.ToHex()</c>).</summary>
    private static WinColor ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);

        byte a = 255;
        if (hex.Length == 8)
        {
            a = Convert.ToByte(hex.Substring(0, 2), 16);
            hex = hex.Substring(2);
        }

        if (hex.Length != 6)
            return Microsoft.UI.Colors.Transparent;

        var r = Convert.ToByte(hex.Substring(0, 2), 16);
        var g = Convert.ToByte(hex.Substring(2, 2), 16);
        var b = Convert.ToByte(hex.Substring(4, 2), 16);
        return WinColor.FromArgb(a, r, g, b);
    }
}
