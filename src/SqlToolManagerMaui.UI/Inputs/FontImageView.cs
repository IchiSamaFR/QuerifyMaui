namespace SqlToolManagerMaui.UI.Inputs;

public class FontImageView : Image
{
    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily),
        typeof(string),
        typeof(FontImageView),
        null,
        propertyChanged: (b, _, _) => ((FontImageView)b).UpdateSource());

    public string FontFamily
    {
        get => (string)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph),
        typeof(string),
        typeof(FontImageView),
        null,
        propertyChanged: (b, _, _) => ((FontImageView)b).UpdateSource());

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color),
        typeof(Color),
        typeof(FontImageView),
        Colors.White,
        propertyChanged: (b, _, _) => ((FontImageView)b).UpdateSource());

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size),
        typeof(double),
        typeof(FontImageView),
        -1d,
        propertyChanged: (b, _, _) => ((FontImageView)b).UpdateSource());

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    private void UpdateSource()
    {
        if (string.IsNullOrEmpty(Glyph) || string.IsNullOrEmpty(FontFamily))
            return;

        Source = new FontImageSource
        {
            FontFamily = FontFamily,
            Glyph = Glyph,
            Color = Color,
            Size = Size,
        };
    }
}