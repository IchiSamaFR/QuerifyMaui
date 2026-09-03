using SqlToolManagerMaui.UI.Inputs.Core;


#if ANDROID
using Android.Widget;
#endif

namespace SqlToolManagerMaui.UI.Inputs;

/// <summary>
/// Champ de saisie une ligne (Entry) avec titre flottant, clear button et validations.
/// Réimplémentation standalone (sans UraniumUI).
/// </summary>
public partial class TextField : InputField
{
    public BorderlessEntry EntryView { get; }

    public override View Content { get; set; } = new BorderlessEntry
    {
        Margin = new Thickness(12, 0),
        BackgroundColor = Colors.Transparent,
        VerticalOptions = LayoutOptions.Center,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalTextAlignment = TextAlignment.Center,
    };

    public override bool HasValue => !string.IsNullOrEmpty(Text);

    public IList<Behavior> EntryBehaviors => EntryView.Behaviors;

    public event EventHandler<TextChangedEventArgs>? TextChanged;

    public event EventHandler? Completed;

    public TextField()
    {
        EntryView = (BorderlessEntry)Content;

        RegisterForEvents();
        BindEntryView();

        EntryView.TextChanged += EntryView_TextChanged;
        EntryView.Completed += EntryView_Completed;
        EntryView.Focused += EntryView_Focused;
    }

    private void BindEntryView()
    {
        EntryView.SetBinding(Entry.TextProperty, new Binding(nameof(Text), BindingMode.TwoWay, source: this));
        EntryView.SetBinding(Entry.TextColorProperty, new Binding(nameof(TextColor), source: this));
        EntryView.SetBinding(InputView.KeyboardProperty, new Binding(nameof(Keyboard), source: this));
        EntryView.SetBinding(Entry.ClearButtonVisibilityProperty, new Binding(nameof(ClearButtonVisibility), source: this));
        EntryView.SetBinding(Entry.IsPasswordProperty, new Binding(nameof(IsPassword), source: this));
        EntryView.SetBinding(Entry.ReturnCommandProperty, new Binding(nameof(ReturnCommand), source: this));
        EntryView.SetBinding(Entry.ReturnCommandParameterProperty, new Binding(nameof(ReturnCommandParameter), source: this));
        EntryView.SetBinding(Entry.ReturnTypeProperty, new Binding(nameof(ReturnType), source: this));
        EntryView.SetBinding(InputView.CharacterSpacingProperty, new Binding(nameof(CharacterSpacing), source: this));
        EntryView.SetBinding(Entry.SelectionLengthProperty, new Binding(nameof(SelectionLength), BindingMode.TwoWay, source: this));
        EntryView.SetBinding(Entry.CursorPositionProperty, new Binding(nameof(CursorPosition), BindingMode.TwoWay, source: this));
        EntryView.SetBinding(InputView.IsTextPredictionEnabledProperty, new Binding(nameof(IsTextPredictionEnabled), source: this));
        EntryView.SetBinding(InputView.MaxLengthProperty, new Binding(nameof(MaxLength), source: this));
        EntryView.SetBinding(InputView.IsReadOnlyProperty, new Binding(nameof(IsReadOnly), source: this));
        EntryView.SetBinding(IsEnabledProperty, new Binding(nameof(IsEnabled), source: this));
        EntryView.SetBinding(Entry.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        EntryView.SetBinding(Entry.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
        EntryView.SetBinding(Entry.FontAttributesProperty, new Binding(nameof(FontAttributes), source: this));
        EntryView.SetBinding(Entry.FontAutoScalingEnabledProperty, new Binding(nameof(FontAutoScalingEnabled), source: this));
        EntryView.SetBinding(Entry.HorizontalTextAlignmentProperty, new Binding(nameof(HorizontalTextAlignment), source: this));
        EntryView.SetBinding(Entry.VerticalTextAlignmentProperty, new Binding(nameof(VerticalTextAlignment), source: this));
        EntryView.SetBinding(Entry.PlaceholderProperty, new Binding(nameof(Placeholder), source: this));
        EntryView.SetBinding(Entry.PlaceholderColorProperty, new Binding(nameof(PlaceholderColor), source: this));
        EntryView.SetBinding(BorderlessEntry.SelectionHighlightColorProperty, new Binding(nameof(SelectionHighlightColor), source: this));
        EntryView.SetBinding(BorderlessEntry.CursorColorProperty, new Binding(nameof(CursorColor), source: this));
    }

    private void EntryView_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.OldTextValue) || string.IsNullOrEmpty(e.NewTextValue))
            UpdateState();

        if (e.NewTextValue is not null)
            CheckAndShowValidations();

        TextChanged?.Invoke(this, e);
    }

    private void EntryView_Completed(object? sender, EventArgs e)
        => Completed?.Invoke(this, e);

    private void EntryView_Focused(object? sender, FocusEventArgs e)
    {
        if (SelectAllTextOnFocus)
            SelectAllText();
    }

    public void ClearValue()
    {
        if (IsEnabled)
            Text = string.Empty;
    }

    protected override object? GetValueForValidator() => EntryView.Text;


    /// <summary>L'input doit déjà avoir le focus quand cette méthode est appelée.</summary>
    public void SelectAllText()
    {
        if (string.IsNullOrEmpty(EntryView.Text))
            return;

        EntryView.CursorPosition = 0;
        EntryView.SelectionLength = EntryView.Text.Length;
#if ANDROID
        if (EntryView.Handler?.PlatformView is EditText editText)
            editText.SelectAll();
#endif
    }
}