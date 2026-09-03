using System.ComponentModel;

namespace SqlToolManagerMaui.UI.Inputs;

public abstract partial class InputField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(InputField), string.Empty,
        propertyChanged: (bindable, _, newValue) =>
        {
            var field = (InputField)bindable;
            field.titleLabel.Text = (string?)newValue ?? string.Empty;
            field.titleLabel.IsVisible = !string.IsNullOrEmpty((string?)newValue);
            field.UpdateSpacerHeight();
            field.UpdateTitlePosition(animate: false);
        });

    public Color TitleColor
    {
        get => (Color)GetValue(TitleColorProperty);
        set => SetValue(TitleColorProperty, value);
    }

    public static readonly BindableProperty TitleColorProperty = BindableProperty.Create(
        nameof(TitleColor), typeof(Color), typeof(InputField), Colors.Gray,
        propertyChanged: (bindable, _, _) => ((InputField)bindable).UpdateColors());

    public double TitleFontSize
    {
        get => (double)GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    public static readonly BindableProperty TitleFontSizeProperty = BindableProperty.Create(
        nameof(TitleFontSize), typeof(double), typeof(InputField), 14d,
        propertyChanged: (bindable, _, newValue) =>
        {
            var field = (InputField)bindable;
            field.titleLabel.FontSize = (double)newValue;
            field.UpdateSpacerHeight();
            field.UpdateTitlePosition(animate: false);
        });

    public double FloatedTitleFontSize
    {
        get => (double)GetValue(FloatedTitleFontSizeProperty);
        set => SetValue(FloatedTitleFontSizeProperty, value);
    }

    public static readonly BindableProperty FloatedTitleFontSizeProperty = BindableProperty.Create(
        nameof(FloatedTitleFontSize), typeof(double), typeof(InputField), 10d,
        propertyChanged: (bindable, _, _) => ((InputField)bindable).UpdateTitlePosition(animate: false));

    public bool IsFloatedTitleFixed
    {
        get => (bool)GetValue(IsFloatedTitleFixedProperty);
        set => SetValue(IsFloatedTitleFixedProperty, value);
    }

    public static readonly BindableProperty IsFloatedTitleFixedProperty = BindableProperty.Create(
        nameof(IsFloatedTitleFixed), typeof(bool), typeof(InputField), false,
        propertyChanged: (bindable, _, _) => ((InputField)bindable).UpdateTitlePosition(animate: false));

    public Color AccentColor
    {
        get => (Color)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor), typeof(Color), typeof(InputField), Colors.RoyalBlue,
        propertyChanged: (bindable, _, _) => ((InputField)bindable).UpdateColors());

    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(InputField), Colors.Gray,
        propertyChanged: (bindable, _, _) => ((InputField)bindable).UpdateColors());

    public Color ErrorColor
    {
        get => (Color)GetValue(ErrorColorProperty);
        set => SetValue(ErrorColorProperty, value);
    }

    public static readonly BindableProperty ErrorColorProperty = BindableProperty.Create(
        nameof(ErrorColor), typeof(Color), typeof(InputField), Colors.Red,
        propertyChanged: (bindable, _, _) => ((InputField)bindable).UpdateColors());

    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
    nameof(BorderThickness), typeof(double), typeof(InputField), 1d,
    propertyChanged: (bindable, _, _) => ((InputField)bindable).UpdateTitlePosition(animate: false));

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(InputField), 8d);

    public Brush? InputBackground
    {
        get => (Brush?)GetValue(InputBackgroundProperty);
        set => SetValue(InputBackgroundProperty, value);
    }

    public static readonly BindableProperty InputBackgroundProperty = BindableProperty.Create(
        nameof(InputBackground), typeof(Brush), typeof(InputField), null);

    public Color? InputBackgroundColor
    {
        get => (Color?)GetValue(InputBackgroundColorProperty);
        set => SetValue(InputBackgroundColorProperty, value);
    }

    public static readonly BindableProperty InputBackgroundColorProperty = BindableProperty.Create(
        nameof(InputBackgroundColor), typeof(Color), typeof(InputField), null);

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(InputField), Colors.White);

    public string? FontFamily
    {
        get => (string?)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily), typeof(string), typeof(InputField), null,
        propertyChanged: (bindable, _, newValue) =>
        {
            var field = (InputField)bindable;
            field.titleLabel.FontFamily = (string?)newValue;
            field.validationLabel.FontFamily = (string?)newValue;
        });

    [TypeConverter(typeof(FontSizeConverter))]
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(InputField), 14d);

    public FontAttributes FontAttributes
    {
        get => (FontAttributes)GetValue(FontAttributesProperty);
        set => SetValue(FontAttributesProperty, value);
    }

    public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(
        nameof(FontAttributes), typeof(FontAttributes), typeof(InputField), FontAttributes.None);

    public bool FontAutoScalingEnabled
    {
        get => (bool)GetValue(FontAutoScalingEnabledProperty);
        set => SetValue(FontAutoScalingEnabledProperty, value);
    }

    public static readonly BindableProperty FontAutoScalingEnabledProperty = BindableProperty.Create(
        nameof(FontAutoScalingEnabled), typeof(bool), typeof(InputField), true);

    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(ImageSource), typeof(InputField), null,
        propertyChanged: (bindable, _, _) => ((InputField)bindable).OnIconChanged());

    /// <summary>
    /// Hauteur du champ (la zone bordée), PAS du contrôle entier :
    /// le titre flottant et le message de validation s'ajoutent autour.
    /// </summary>
    public new double HeightRequest
    {
        get => (double)GetValue(HeightRequestProperty);
        set => SetValue(HeightRequestProperty, value);
    }

    public new static readonly BindableProperty HeightRequestProperty = BindableProperty.Create(
        nameof(HeightRequest), typeof(double), typeof(InputField), -1d,
        propertyChanged: (bindable, _, _) => ((InputField)bindable).UpdateTitlePosition(animate: false));

    /// <summary>Hauteur maximale du champ bordé (utile pour EditorField en AutoSize).</summary>
    public new double MaximumHeightRequest
    {
        get => (double)GetValue(MaximumHeightRequestProperty);
        set => SetValue(MaximumHeightRequestProperty, value);
    }

    public new static readonly BindableProperty MaximumHeightRequestProperty = BindableProperty.Create(
        nameof(MaximumHeightRequest), typeof(double), typeof(InputField), double.PositiveInfinity);
}
