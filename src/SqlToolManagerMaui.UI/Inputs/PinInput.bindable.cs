using SqlToolManagerMaui.UI.Inputs.Enums;
using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class PinInput
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(string), typeof(PinInput), string.Empty, BindingMode.TwoWay,
        propertyChanged: (bindable, _, newValue) =>
            ((PinInput)bindable).OnValueChanged((string?)newValue ?? string.Empty));

    public int Length
    {
        get => (int)GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    public static readonly BindableProperty LengthProperty = BindableProperty.Create(
        nameof(Length), typeof(int), typeof(PinInput), 4,
        propertyChanged: (bindable, _, _) =>
        {
            var pin = (PinInput)bindable;
            pin.ApplyKeyboard();
            pin.BuildBoxes();
        });

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(
        nameof(IsPassword), typeof(bool), typeof(PinInput), false,
        propertyChanged: (bindable, _, _) => ((PinInput)bindable).UpdateBoxes());

    public PinInputKeyboardType KeyboardType
    {
        get => (PinInputKeyboardType)GetValue(KeyboardTypeProperty);
        set => SetValue(KeyboardTypeProperty, value);
    }

    public static readonly BindableProperty KeyboardTypeProperty = BindableProperty.Create(
        nameof(KeyboardType), typeof(PinInputKeyboardType), typeof(PinInput), PinInputKeyboardType.Numeric,
        propertyChanged: (bindable, _, _) => ((PinInput)bindable).ApplyKeyboard());

    public ICommand? CompletedCommand
    {
        get => (ICommand?)GetValue(CompletedCommandProperty);
        set => SetValue(CompletedCommandProperty, value);
    }

    public static readonly BindableProperty CompletedCommandProperty = BindableProperty.Create(
        nameof(CompletedCommand), typeof(ICommand), typeof(PinInput), null);

    public object? CompletedCommandParameter
    {
        get => GetValue(CompletedCommandParameterProperty);
        set => SetValue(CompletedCommandParameterProperty, value);
    }

    public static readonly BindableProperty CompletedCommandParameterProperty = BindableProperty.Create(
        nameof(CompletedCommandParameter), typeof(object), typeof(PinInput), null);

    // ── Apparence ─────────────────────────────────────────────────────────────

    public PinInputBoxShape Shape
    {
        get => (PinInputBoxShape)GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public static readonly BindableProperty ShapeProperty = BindableProperty.Create(
    nameof(Shape), typeof(PinInputBoxShape), typeof(PinInput), PinInputBoxShape.RoundCorner);

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(PinInput), 48d);

    public double BoxSpacing
    {
        get => (double)GetValue(BoxSpacingProperty);
        set => SetValue(BoxSpacingProperty, value);
    }

    public static readonly BindableProperty BoxSpacingProperty = BindableProperty.Create(
        nameof(BoxSpacing), typeof(double), typeof(PinInput), 8d);

    public double BoxCornerRadius
    {
        get => (double)GetValue(BoxCornerRadiusProperty);
        set => SetValue(BoxCornerRadiusProperty, value);
    }

    /// <summary>Rayon des boîtes. -1 (défaut) = hérite de <see cref="InputField.CornerRadius"/>.</summary>
    public static readonly BindableProperty BoxCornerRadiusProperty = BindableProperty.Create(
        nameof(BoxCornerRadius), typeof(double), typeof(PinInput), -1d);

    public double BoxBorderThickness
    {
        get => (double)GetValue(BoxBorderThicknessProperty);
        set => SetValue(BoxBorderThicknessProperty, value);
    }

    /// <summary>Épaisseur du trait des boîtes. -1 (défaut) = hérite de <see cref="InputField.BorderThickness"/>.</summary>
    public static readonly BindableProperty BoxBorderThicknessProperty = BindableProperty.Create(
        nameof(BoxBorderThickness), typeof(double), typeof(PinInput), -1d,
        propertyChanged: (bindable, _, _) => ((PinInput)bindable).UpdateBoxes());

    public Color? BoxBorderColor
    {
        get => (Color?)GetValue(BoxBorderColorProperty);
        set => SetValue(BoxBorderColorProperty, value);
    }

    /// <summary>Couleur du trait des boîtes. null (défaut) = hérite de <see cref="InputField.BorderColor"/>.</summary>
    public static readonly BindableProperty BoxBorderColorProperty = BindableProperty.Create(
        nameof(BoxBorderColor), typeof(Color), typeof(PinInput), null,
        propertyChanged: (bindable, _, _) => ((PinInput)bindable).UpdateBoxes());

    // ── Valeurs effectives (héritage des propriétés d'InputField) ─────────────

    internal double EffectiveBoxCornerRadius => BoxCornerRadius >= 0 ? BoxCornerRadius : CornerRadius;

    internal double EffectiveBoxBorderThickness => BoxBorderThickness >= 0 ? BoxBorderThickness : BorderThickness;

    internal Color EffectiveBoxBorderColor => BoxBorderColor ?? BorderColor;
}
