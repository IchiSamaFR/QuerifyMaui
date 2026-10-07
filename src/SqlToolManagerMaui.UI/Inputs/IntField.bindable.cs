using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class IntField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, CoerceValueMethodName = nameof(CoerceValue), PropertyChangedMethodName = nameof(OnValueChanged))]
    public partial int Value { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnRangeChanged))]
    public partial int Minimum { get; set; } = int.MinValue;

    [BindableProperty(PropertyChangedMethodName = nameof(OnRangeChanged))]
    public partial int Maximum { get; set; } = int.MaxValue;

    [BindableProperty]
    public partial int Step { get; set; } = 1;

    [BindableProperty(PropertyChangedMethodName = nameof(OnStepStateChanged))]
    public partial bool IsReadOnly { get; set; }

    /// <summary>Couleur du texte/teinte des boutons "−" / "+" lorsqu'ils sont actifs.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnStepStateChanged))]
    public partial Color StepEnabledColor { get; set; } = Colors.DeepPink;

    /// <summary>Couleur du texte/teinte des boutons "−" / "+" lorsqu'ils sont désactivés (borne atteinte / lecture seule).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnStepStateChanged))]
    public partial Color StepDisabledColor { get; set; } = Colors.Gray;

    /// <summary>Image personnalisée du bouton "−". Si définie, le texte du bouton est vidé.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnMinusImageSourceChanged))]
    public partial ImageSource? MinusImageSource { get; set; }

    /// <summary>Image personnalisée du bouton "+". Si définie, le texte du bouton est vidé.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnAddImageSourceChanged))]
    public partial ImageSource? AddImageSource { get; set; }

    /// <summary>Couleur de fond des boutons "−" / "+".</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnStepStateChanged))]
    public partial Color StepBackgroundColor { get; set; } = Colors.Transparent;

    /// <summary>Couleur de fond des boutons "−" / "+" lorsqu'ils sont désactivés (borne atteinte / lecture seule).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnStepStateChanged))]
    public partial Color StepDisabledBackgroundColor { get; set; } = Colors.Transparent;

    /// <summary>Couleur de surbrillance de la sélection dans l'entry.</summary>
    [BindableProperty]
    public partial Color? SelectionHighlightColor { get; set; }
}
