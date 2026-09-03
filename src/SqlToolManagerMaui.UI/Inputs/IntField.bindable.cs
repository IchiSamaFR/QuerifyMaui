namespace SqlToolManagerMaui.UI.Inputs;

public partial class IntField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(int), typeof(IntField), 0, BindingMode.TwoWay,
        coerceValue: CoerceValue, propertyChanged: OnValueChanged);

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly BindableProperty MinimumProperty = BindableProperty.Create(
        nameof(Minimum), typeof(int), typeof(IntField), int.MinValue,
        propertyChanged: OnRangeChanged);

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly BindableProperty MaximumProperty = BindableProperty.Create(
        nameof(Maximum), typeof(int), typeof(IntField), int.MaxValue,
        propertyChanged: OnRangeChanged);

    public int Step
    {
        get => (int)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public static readonly BindableProperty StepProperty = BindableProperty.Create(
        nameof(Step), typeof(int), typeof(IntField), 1);

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
        nameof(IsReadOnly), typeof(bool), typeof(IntField), false,
        propertyChanged: OnStepStateChanged);

    /// <summary>Couleur du texte/teinte des boutons "−" / "+" lorsqu'ils sont actifs.</summary>
    public Color StepEnabledColor
    {
        get => (Color)GetValue(StepEnabledColorProperty);
        set => SetValue(StepEnabledColorProperty, value);
    }

    public static readonly BindableProperty StepEnabledColorProperty = BindableProperty.Create(
        nameof(StepEnabledColor), typeof(Color), typeof(IntField), Colors.DeepPink,
        propertyChanged: OnStepStateChanged);

    /// <summary>Couleur du texte/teinte des boutons "−" / "+" lorsqu'ils sont désactivés (borne atteinte / lecture seule).</summary>
    public Color StepDisabledColor
    {
        get => (Color)GetValue(StepDisabledColorProperty);
        set => SetValue(StepDisabledColorProperty, value);
    }

    public static readonly BindableProperty StepDisabledColorProperty = BindableProperty.Create(
        nameof(StepDisabledColor), typeof(Color), typeof(IntField), Colors.Gray,
        propertyChanged: OnStepStateChanged);

    /// <summary>Image personnalisée du bouton "−". Si définie, le texte du bouton est vidé.</summary>
    public ImageSource? MinusImageSource
    {
        get => (ImageSource?)GetValue(MinusImageSourceProperty);
        set => SetValue(MinusImageSourceProperty, value);
    }

    public static readonly BindableProperty MinusImageSourceProperty = BindableProperty.Create(
        nameof(MinusImageSource), typeof(ImageSource), typeof(IntField), null,
        propertyChanged: OnMinusImageSourceChanged);

    /// <summary>Image personnalisée du bouton "+". Si définie, le texte du bouton est vidé.</summary>
    public ImageSource? AddImageSource
    {
        get => (ImageSource?)GetValue(AddImageSourceProperty);
        set => SetValue(AddImageSourceProperty, value);
    }

    public static readonly BindableProperty AddImageSourceProperty = BindableProperty.Create(
        nameof(AddImageSource), typeof(ImageSource), typeof(IntField), null,
        propertyChanged: OnAddImageSourceChanged);

    /// <summary>Couleur de fond des boutons "−" / "+".</summary>
    public Color StepBackgroundColor
    {
        get => (Color)GetValue(StepBackgroundColorProperty);
        set => SetValue(StepBackgroundColorProperty, value);
    }

    public static readonly BindableProperty StepBackgroundColorProperty = BindableProperty.Create(
        nameof(StepBackgroundColor), typeof(Color), typeof(IntField), Colors.Transparent,
        propertyChanged: OnStepStateChanged);

    /// <summary>Couleur de fond des boutons "−" / "+" lorsqu'ils sont désactivés (borne atteinte / lecture seule).</summary>
    public Color StepDisabledBackgroundColor
    {
        get => (Color)GetValue(StepDisabledBackgroundColorProperty);
        set => SetValue(StepDisabledBackgroundColorProperty, value);
    }

    public static readonly BindableProperty StepDisabledBackgroundColorProperty = BindableProperty.Create(
        nameof(StepDisabledBackgroundColor), typeof(Color), typeof(IntField), Colors.Transparent,
        propertyChanged: OnStepStateChanged);

    /// <summary>Couleur de surbrillance de la sélection dans l'entry.</summary>
    public Color? SelectionHighlightColor
    {
        get => (Color?)GetValue(SelectionHighlightColorProperty);
        set => SetValue(SelectionHighlightColorProperty, value);
    }

    public static readonly BindableProperty SelectionHighlightColorProperty = BindableProperty.Create(
        nameof(SelectionHighlightColor), typeof(Color), typeof(IntField), null);
}
