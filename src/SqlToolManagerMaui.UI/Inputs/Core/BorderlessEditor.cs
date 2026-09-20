namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// Versions "nues" des inputs natifs (sans underline Android / bordure iOS),
/// </summary>
public class BorderlessEditor : Editor
{
    static BorderlessEditor() => BorderlessHandlers.Init();

    public static readonly BindableProperty CursorColorProperty = BindableProperty.Create(
        nameof(CursorColor),
        typeof(Color),
        typeof(BorderlessEditor),
        null,
        propertyChanged: (bindable, _, _) => ((BorderlessEditor)bindable).Handler?.UpdateValue(nameof(CursorColor)));

    public Color? CursorColor
    {
        get => (Color?)GetValue(CursorColorProperty);
        set => SetValue(CursorColorProperty, value);
    }

    /// <summary>
    /// Couleur d'accent du champ. Sur Windows, elle remplace l'accent systeme utilise par WinUI
    /// pour la bordure basse du TextBox quand il a le focus (bleu par defaut).
    /// </summary>
    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor),
        typeof(Color),
        typeof(BorderlessEditor),
        null,
        propertyChanged: (bindable, _, _) => ((BorderlessEditor)bindable).Handler?.UpdateValue(nameof(AccentColor)));

    public Color? AccentColor
    {
        get => (Color?)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }
}