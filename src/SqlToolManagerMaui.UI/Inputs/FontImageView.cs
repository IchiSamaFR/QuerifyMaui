using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class FontImageView : Image
{
    [BindableProperty(PropertyChangedMethodName = nameof(OnSourcePropertyChanged))]
    public partial string FontFamily { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnSourcePropertyChanged))]
    public partial string Glyph { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnSourcePropertyChanged))]
    public partial Color Color { get; set; } = Colors.White;

    [BindableProperty(PropertyChangedMethodName = nameof(OnSourcePropertyChanged))]
    public partial double Size { get; set; } = -1d;

    private static void OnSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((FontImageView)bindable).UpdateSource();

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