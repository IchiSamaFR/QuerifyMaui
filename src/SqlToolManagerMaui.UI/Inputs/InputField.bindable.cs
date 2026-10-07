using CommunityToolkit.Maui;
using System.ComponentModel;

namespace SqlToolManagerMaui.UI.Inputs;

public abstract partial class InputField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    [BindableProperty(PropertyChangedMethodName = nameof(OnTitleChanged))]
    public partial string Title { get; set; } = string.Empty;

    [BindableProperty(PropertyChangedMethodName = nameof(OnColorPropertyChanged))]
    public partial Color TitleColor { get; set; } = Colors.Gray;

    [BindableProperty(PropertyChangedMethodName = nameof(OnTitleFontSizeChanged))]
    public partial double TitleFontSize { get; set; } = 14d;

    [BindableProperty(PropertyChangedMethodName = nameof(OnTitleLayoutPropertyChanged))]
    public partial double FloatedTitleFontSize { get; set; } = 10d;

    [BindableProperty(PropertyChangedMethodName = nameof(OnTitleLayoutPropertyChanged))]
    public partial bool IsFloatedTitleFixed { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnColorPropertyChanged))]
    public partial Color AccentColor { get; set; } = Colors.RoyalBlue;

    [BindableProperty(PropertyChangedMethodName = nameof(OnColorPropertyChanged))]
    public partial Color BorderColor { get; set; } = Colors.Gray;

    [BindableProperty(PropertyChangedMethodName = nameof(OnColorPropertyChanged))]
    public partial Color ErrorColor { get; set; } = Colors.Red;

    [BindableProperty(PropertyChangedMethodName = nameof(OnTitleLayoutPropertyChanged))]
    public partial double BorderThickness { get; set; } = 1d;

    [BindableProperty]
    public partial double CornerRadius { get; set; } = 8d;

    [BindableProperty]
    public partial Brush? InputBackground { get; set; }

    [BindableProperty]
    public partial Color? InputBackgroundColor { get; set; }

    [BindableProperty]
    public partial Color TextColor { get; set; } = Colors.White;

    [BindableProperty(PropertyChangedMethodName = nameof(OnFontFamilyChanged))]
    public partial string? FontFamily { get; set; }

    [TypeConverter(typeof(FontSizeConverter))]
    [BindableProperty]
    public partial double FontSize { get; set; } = 14d;

    [BindableProperty]
    public partial FontAttributes FontAttributes { get; set; } = FontAttributes.None;

    [BindableProperty]
    public partial bool FontAutoScalingEnabled { get; set; } = true;

    [BindableProperty(PropertyChangedMethodName = nameof(OnIconPropertyChanged))]
    public partial ImageSource? Icon { get; set; }

    /// <summary>
    /// Hauteur du champ (la zone bordée), PAS du contrôle entier :
    /// le titre flottant et le message de validation s'ajoutent autour.
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnTitleLayoutPropertyChanged))]
    public new partial double HeightRequest { get; set; } = -1d;

    /// <summary>Hauteur maximale du champ bordé (utile pour EditorField en AutoSize).</summary>
    [BindableProperty]
    public new partial double MaximumHeightRequest { get; set; } = double.PositiveInfinity;

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var field = (InputField)bindable;
        field.titleLabel.Text = (string?)newValue ?? string.Empty;
        field.titleLabel.IsVisible = !string.IsNullOrEmpty((string?)newValue);
        field.UpdateSpacerHeight();
        field.UpdateTitlePosition(animate: false);
    }

    private static void OnTitleFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var field = (InputField)bindable;
        field.titleLabel.FontSize = (double)newValue;
        field.UpdateSpacerHeight();
        field.UpdateTitlePosition(animate: false);
    }

    private static void OnFontFamilyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var field = (InputField)bindable;
        field.titleLabel.FontFamily = (string?)newValue;
        field.validationLabel.FontFamily = (string?)newValue;
    }

    private static void OnTitleLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((InputField)bindable).UpdateTitlePosition(animate: false);

    private static void OnColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((InputField)bindable).UpdateColors();

    private static void OnIconPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((InputField)bindable).OnIconChanged();
}
