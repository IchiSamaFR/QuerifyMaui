using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// Versions "nues" des inputs natifs (sans underline Android / bordure iOS),
/// </summary>
public partial class BorderlessEditor : Editor
{
    static BorderlessEditor() => BorderlessHandlers.Init();

    [BindableProperty(PropertyChangedMethodName = nameof(OnCursorColorChanged))]
    public partial Color? CursorColor { get; set; }

    /// <summary>
    /// Couleur d'accent du champ. Sur Windows, elle remplace l'accent systeme utilise par WinUI
    /// pour la bordure basse du TextBox quand il a le focus (bleu par defaut).
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnAccentColorChanged))]
    public partial Color? AccentColor { get; set; }

    private static void OnCursorColorChanged(BindableObject bindable, object oldValue, object newValue)
        => ((BorderlessEditor)bindable).Handler?.UpdateValue(nameof(CursorColor));

    private static void OnAccentColorChanged(BindableObject bindable, object oldValue, object newValue)
        => ((BorderlessEditor)bindable).Handler?.UpdateValue(nameof(AccentColor));
}