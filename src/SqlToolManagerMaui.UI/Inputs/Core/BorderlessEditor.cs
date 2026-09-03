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
}