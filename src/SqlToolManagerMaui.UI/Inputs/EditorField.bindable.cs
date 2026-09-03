using Microsoft.Maui.Converters;
using SqlToolManagerMaui.UI.Inputs.Core;
using System.ComponentModel;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class EditorField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
        nameof(Placeholder), typeof(string), typeof(EditorField), string.Empty, BindingMode.TwoWay);

    public Color PlaceholderColor
    {
        get => (Color)GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
    }

    public static readonly BindableProperty PlaceholderColorProperty = BindableProperty.Create(
        nameof(PlaceholderColor), typeof(Color), typeof(EditorField),
        InputColors.Get("OnBackground", "OnBackgroundDark", Colors.DarkGray));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(EditorField), string.Empty, BindingMode.TwoWay);


    [TypeConverter(typeof(KeyboardTypeConverter))]
    public Keyboard Keyboard
    {
        get => (Keyboard)GetValue(KeyboardProperty);
        set => SetValue(KeyboardProperty, value);
    }

    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(
        nameof(Keyboard), typeof(Keyboard), typeof(EditorField), Keyboard.Default);

    public double CharacterSpacing
    {
        get => (double)GetValue(CharacterSpacingProperty);
        set => SetValue(CharacterSpacingProperty, value);
    }

    public static readonly BindableProperty CharacterSpacingProperty = BindableProperty.Create(
        nameof(CharacterSpacing), typeof(double), typeof(EditorField), 0d);

    public int SelectionLength
    {
        get => (int)GetValue(SelectionLengthProperty);
        set => SetValue(SelectionLengthProperty, value);
    }

    public static readonly BindableProperty SelectionLengthProperty = BindableProperty.Create(
        nameof(SelectionLength), typeof(int), typeof(EditorField), 0, BindingMode.TwoWay);

    public Color? CursorColor
    {
        get => (Color?)GetValue(CursorColorProperty);
        set => SetValue(CursorColorProperty, value);
    }

    public static readonly BindableProperty CursorColorProperty = BindableProperty.Create(
        nameof(CursorColor), typeof(Color), typeof(EditorField), null);

    public int CursorPosition
    {
        get => (int)GetValue(CursorPositionProperty);
        set => SetValue(CursorPositionProperty, value);
    }

    public static readonly BindableProperty CursorPositionProperty = BindableProperty.Create(
        nameof(CursorPosition), typeof(int), typeof(EditorField), 0, BindingMode.TwoWay);

    public int MaxLength
    {
        get => (int)GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(
        nameof(MaxLength), typeof(int), typeof(EditorField), int.MaxValue);

    public TextAlignment HorizontalTextAlignment
    {
        get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
        set => SetValue(HorizontalTextAlignmentProperty, value);
    }

    public static readonly BindableProperty HorizontalTextAlignmentProperty = BindableProperty.Create(
        nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(EditorField), TextAlignment.Start);

    public TextAlignment VerticalTextAlignment
    {
        get => (TextAlignment)GetValue(VerticalTextAlignmentProperty);
        set => SetValue(VerticalTextAlignmentProperty, value);
    }

    public static readonly BindableProperty VerticalTextAlignmentProperty = BindableProperty.Create(
        nameof(VerticalTextAlignment), typeof(TextAlignment), typeof(EditorField), TextAlignment.Center);


    public bool IsTextPredictionEnabled
    {
        get => (bool)GetValue(IsTextPredictionEnabledProperty);
        set => SetValue(IsTextPredictionEnabledProperty, value);
    }

    public static readonly BindableProperty IsTextPredictionEnabledProperty = BindableProperty.Create(
        nameof(IsTextPredictionEnabled), typeof(bool), typeof(EditorField), true);

    public bool IsSpellCheckEnabled
    {
        get => (bool)GetValue(IsSpellCheckEnabledProperty);
        set => SetValue(IsSpellCheckEnabledProperty, value);
    }

    public static readonly BindableProperty IsSpellCheckEnabledProperty = BindableProperty.Create(
        nameof(IsSpellCheckEnabled), typeof(bool), typeof(EditorField), false);

    public double BaseHeightRequest
    {
        get => (double)GetValue(BaseHeightRequestProperty);
        set => SetValue(BaseHeightRequestProperty, value);
    }

    public static readonly BindableProperty BaseHeightRequestProperty = BindableProperty.Create(
        nameof(BaseHeightRequest), typeof(double), typeof(EditorField), 0d,
        propertyChanged: (bindable, _, _) => ((EditorField)bindable).ApplyHeights());

    public double HeightByLines
    {
        get => (double)GetValue(HeightByLinesProperty);
        set => SetValue(HeightByLinesProperty, value);
    }

    public static readonly BindableProperty HeightByLinesProperty = BindableProperty.Create(
        nameof(HeightByLines), typeof(double), typeof(EditorField), 0d,
        propertyChanged: (bindable, _, _) => ((EditorField)bindable).ApplyHeights());

    public double MinimumHeightByLines
    {
        get => (double)GetValue(MinimumHeightByLinesProperty);
        set => SetValue(MinimumHeightByLinesProperty, value);
    }

    public static readonly BindableProperty MinimumHeightByLinesProperty = BindableProperty.Create(
        nameof(MinimumHeightByLines), typeof(double), typeof(EditorField), 0d,
        propertyChanged: (bindable, _, _) => ((EditorField)bindable).ApplyHeights());

    public double MaximumHeightByLines
    {
        get => (double)GetValue(MaximumHeightByLinesProperty);
        set => SetValue(MaximumHeightByLinesProperty, value);
    }

    public static readonly BindableProperty MaximumHeightByLinesProperty = BindableProperty.Create(
        nameof(MaximumHeightByLines), typeof(double), typeof(EditorField), 0d,
        propertyChanged: (bindable, _, _) => ((EditorField)bindable).ApplyHeights());
}
