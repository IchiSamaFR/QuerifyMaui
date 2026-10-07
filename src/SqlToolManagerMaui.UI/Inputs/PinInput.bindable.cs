using CommunityToolkit.Maui;
using SqlToolManagerMaui.UI.Inputs.Enums;
using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class PinInput
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnValuePropertyChanged))]
    public partial string Value { get; set; } = string.Empty;

    [BindableProperty(PropertyChangedMethodName = nameof(OnLengthChanged))]
    public partial int Length { get; set; } = 4;

    [BindableProperty(PropertyChangedMethodName = nameof(OnBoxesPropertyChanged))]
    public partial bool IsPassword { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnKeyboardTypeChanged))]
    public partial PinInputKeyboardType KeyboardType { get; set; } = PinInputKeyboardType.Numeric;

    [BindableProperty]
    public partial ICommand? CompletedCommand { get; set; }

    [BindableProperty]
    public partial object? CompletedCommandParameter { get; set; }

    // ── Apparence ─────────────────────────────────────────────────────────────

    [BindableProperty]
    public partial PinInputBoxShape Shape { get; set; } = PinInputBoxShape.RoundCorner;

    [BindableProperty]
    public partial double Size { get; set; } = 48d;

    [BindableProperty]
    public partial double BoxSpacing { get; set; } = 8d;

    /// <summary>Rayon des boîtes. -1 (défaut) = hérite de <see cref="InputField.CornerRadius"/>.</summary>
    [BindableProperty]
    public partial double BoxCornerRadius { get; set; } = -1d;

    /// <summary>Épaisseur du trait des boîtes. -1 (défaut) = hérite de <see cref="InputField.BorderThickness"/>.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnBoxesPropertyChanged))]
    public partial double BoxBorderThickness { get; set; } = -1d;

    /// <summary>Couleur du trait des boîtes. null (défaut) = hérite de <see cref="InputField.BorderColor"/>.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnBoxesPropertyChanged))]
    public partial Color? BoxBorderColor { get; set; }

    // ── Valeurs effectives (héritage des propriétés d'InputField) ─────────────

    internal double EffectiveBoxCornerRadius => BoxCornerRadius >= 0 ? BoxCornerRadius : CornerRadius;

    internal double EffectiveBoxBorderThickness => BoxBorderThickness >= 0 ? BoxBorderThickness : BorderThickness;

    internal Color EffectiveBoxBorderColor => BoxBorderColor ?? BorderColor;

    private static void OnValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((PinInput)bindable).OnValueChanged((string?)newValue ?? string.Empty);

    private static void OnLengthChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var pin = (PinInput)bindable;
        pin.ApplyKeyboard();
        pin.BuildBoxes();
    }

    private static void OnKeyboardTypeChanged(BindableObject bindable, object oldValue, object newValue)
        => ((PinInput)bindable).ApplyKeyboard();

    private static void OnBoxesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((PinInput)bindable).UpdateBoxes();
}
