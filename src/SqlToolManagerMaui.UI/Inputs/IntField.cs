using SqlToolManagerMaui.UI.Inputs.Core;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Inputs;

/// <summary>
/// Champ numérique entier de type "stepper" basé sur <see cref="InputField"/> :
/// un bouton "−" à gauche, la valeur centrée éditable au milieu et un bouton "+" à droite,
/// le tout dans la bordure arrondie commune aux inputs.
///
/// Contrôle 100% code (visuel + logique dans ce seul fichier).
/// La valeur est bornée par <see cref="Minimum"/> / <see cref="Maximum"/> et incrémentée
/// par pas de <see cref="Step"/>.
/// </summary>
public partial class IntField : InputField
{
    private bool _syncingText;

    private readonly BorderlessEntry _entry;
    private readonly Button _decrementButton = CreateStepButton("−");
    private readonly Button _incrementButton = CreateStepButton("+");

    public BorderlessEntry EntryView => _entry;

    public override View Content { get; set; } = null!;

    public override bool HasValue => true;

    public event EventHandler<int>? ValueChanged;

    /// <summary>Commande déclenchée par le bouton "−".</summary>
    public ICommand DecrementCommand { get; }

    /// <summary>Commande déclenchée par le bouton "+".</summary>
    public ICommand IncrementCommand { get; }

    public IntField()
    {
        DecrementCommand = new Command(Decrement);
        IncrementCommand = new Command(Increment);

        _entry = CreateEntry();
        _decrementButton.Command = DecrementCommand;
        _incrementButton.Command = IncrementCommand;

        ApplyStepImage(_decrementButton, MinusImageSource, "−");
        ApplyStepImage(_incrementButton, AddImageSource, "+");

        Content = _entry;

        startIconsContainer.Add(_decrementButton);
        endIconsContainer.Add(_incrementButton);

        RegisterForEvents();

        EntryView.TextChanged += EntryView_TextChanged;
        EntryView.Completed += (_, _) => CommitText();
        EntryView.Unfocused += (_, _) => CommitText();

        SyncTextFromValue();
        UpdateButtonsState();
    }

    // ── Construction du visuel ────────────────────────────────────────────────

    private BorderlessEntry CreateEntry()
    {
        var entry = new BorderlessEntry
        {
            Margin = new Thickness(4, 0),
            BackgroundColor = Colors.Transparent,
            Keyboard = Keyboard.Numeric,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        entry.SetBinding(Entry.TextColorProperty, new Binding(nameof(TextColor), source: this));
        entry.SetBinding(Entry.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        entry.SetBinding(Entry.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
        entry.SetBinding(Entry.FontAttributesProperty, new Binding(nameof(FontAttributes), source: this));

        entry.SetBinding(InputView.IsReadOnlyProperty, new Binding(nameof(IsReadOnly), source: this));

        entry.SetBinding(BorderlessEntry.SelectionHighlightColorProperty, new Binding(nameof(SelectionHighlightColor), source: this));

        entry.SetBinding(IsEnabledProperty, new Binding(nameof(IsEnabled), source: this));

        return entry;
    }

    private static Button CreateStepButton(string glyph)
    {
        var button = new Button
        {
            Text = glyph,
            FontSize = 26,
        };

        button.SetDynamicResource(StyleProperty, "TextButtonStyle");

        return button;
    }

    /// <summary>
    /// Applique l'<see cref="ImageSource"/> d'un bouton step. Si une image est fournie,
    /// le texte du bouton est vidé ; sinon le glyphe par défaut est restauré.
    /// </summary>
    private void ApplyStepImage(Button button, ImageSource? imageSource, string fallbackGlyph)
    {
        button.ImageSource = imageSource;

        if (imageSource is not null)
            button.Text = string.Empty;
        else if (string.IsNullOrEmpty(button.Text))
            button.Text = fallbackGlyph;
    }

    // ── Logique numérique ─────────────────────────────────────────────────────

    public void Increment() => SetValueClamped(Value + Step);

    public void Decrement() => SetValueClamped(Value - Step);

    private void SetValueClamped(int candidate)
    {
        if (IsReadOnly || !IsEnabled)
            return;

        Value = Math.Clamp(candidate, Minimum, Maximum);
    }

    private void EntryView_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncingText)
            return;

        // Édition libre pendant la frappe ; le clamp/format se fait au commit.
        if (int.TryParse(e.NewTextValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed))
        {
            _syncingText = true;
            Value = Math.Clamp(parsed, Minimum, Maximum);
            _syncingText = false;
        }
    }

    private void CommitText()
    {
        SyncTextFromValue();
        CheckAndShowValidations();
    }

    private void UpdateButtonsState()
    {
        bool editable = IsEnabled && !IsReadOnly;

        ApplyButtonState(_decrementButton, editable && Value > Minimum);
        ApplyButtonState(_incrementButton, editable && Value < Maximum);
    }

    private void ApplyButtonState(Button button, bool enabled)
    {
        var color = enabled ? StepEnabledColor : StepDisabledColor;

        button.IsEnabled = enabled;
        button.BackgroundColor = enabled ? StepBackgroundColor : StepDisabledBackgroundColor;
        button.TextColor = color;

        // Applique également la couleur à l'image personnalisée (glyphe de police).
        if (button.ImageSource is FontImageSource fontImage)
            fontImage.Color = color;
    }

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsEnabledProperty.PropertyName)
            UpdateButtonsState();
    }

    private void SyncTextFromValue()
    {
        _syncingText = true;
        EntryView.Text = Value.ToString(CultureInfo.CurrentCulture);
        _syncingText = false;
    }

    protected override object? GetValueForValidator() => Value;

    // ── Handlers de propriétés (nommés, sans lambda) ──────────────────────────

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var field = (IntField)bindable;

        if (!field._syncingText)
            field.SyncTextFromValue();

        field.UpdateButtonsState();
        field.CheckAndShowValidations();
        field.ValueChanged?.Invoke(field, (int)newValue);
    }

    private static object CoerceValue(BindableObject bindable, object value)
    {
        var field = (IntField)bindable;
        return Math.Clamp((int)value, field.Minimum, field.Maximum);
    }

    private static void OnRangeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var field = (IntField)bindable;
        field.Value = Math.Clamp(field.Value, field.Minimum, field.Maximum);
        field.UpdateButtonsState();
    }

    private static void OnStepStateChanged(BindableObject bindable, object oldValue, object newValue)
        => ((IntField)bindable).UpdateButtonsState();

    private static void OnMinusImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var field = (IntField)bindable;
        field.ApplyStepImage(field._decrementButton, (ImageSource?)newValue, "−");
        field.UpdateButtonsState();
    }

    private static void OnAddImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var field = (IntField)bindable;
        field.ApplyStepImage(field._incrementButton, (ImageSource?)newValue, "+");
        field.UpdateButtonsState();
    }

}
