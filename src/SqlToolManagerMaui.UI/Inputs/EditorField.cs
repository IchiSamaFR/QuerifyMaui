using SqlToolManagerMaui.UI.Inputs.Core;
using System.Runtime.CompilerServices;

namespace SqlToolManagerMaui.UI.Inputs;

/// <summary>
/// Champ de saisie multi-lignes (Editor) avec titre flottant et validations.
/// Réimplémentation standalone (sans UraniumUI).
/// </summary>
[ContentProperty(nameof(Validations))]
public partial class EditorField : InputField
{
    // Fallback utilisé avant que le Handler natif ne soit disponible (ex : parsing XAML).
    private const double _fallbackLineHeightMultiplier = 1.36;

    private double? _cachedLineHeight;

    public BorderlessEditor EditorView { get; }

    public override View Content { get; set; } = new BorderlessEditor
    {
        Margin = new Thickness(0, 0),
        BackgroundColor = Colors.Transparent,
        AutoSize = EditorAutoSizeOption.TextChanges,
        VerticalOptions = LayoutOptions.Start,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalTextAlignment = TextAlignment.Center,
    };

    public override bool HasValue => !string.IsNullOrEmpty(Text);

    protected override double ContentTextStartInset => 10;

    public event EventHandler<TextChangedEventArgs>? TextChanged;

    public event EventHandler? Completed;

    public EditorField()
    {
        EditorView = (BorderlessEditor)Content;

        RegisterForEvents();
        BindEditorView();

        EditorView.TextChanged += OnEditorTextChanged;
        EditorView.Completed += OnEditorCompleted;
    }

    private void BindEditorView()
    {
        EditorView.SetBinding(Editor.TextProperty, new Binding(nameof(Text), BindingMode.TwoWay, source: this));
        EditorView.SetBinding(Editor.TextColorProperty, new Binding(nameof(TextColor), source: this));
        EditorView.SetBinding(Editor.SelectionLengthProperty, new Binding(nameof(SelectionLength), BindingMode.TwoWay, source: this));
        EditorView.SetBinding(Editor.CursorPositionProperty, new Binding(nameof(CursorPosition), BindingMode.TwoWay, source: this));
        EditorView.SetBinding(Editor.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        EditorView.SetBinding(Editor.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
        EditorView.SetBinding(Editor.FontAttributesProperty, new Binding(nameof(FontAttributes), source: this));
        EditorView.SetBinding(Editor.FontAutoScalingEnabledProperty, new Binding(nameof(FontAutoScalingEnabled), source: this));
        EditorView.SetBinding(Editor.HorizontalTextAlignmentProperty, new Binding(nameof(HorizontalTextAlignment), source: this));
        EditorView.SetBinding(Editor.VerticalTextAlignmentProperty, new Binding(nameof(VerticalTextAlignment), source: this));
        EditorView.SetBinding(Editor.PlaceholderProperty, new Binding(nameof(Placeholder), source: this));
        EditorView.SetBinding(Editor.PlaceholderColorProperty, new Binding(nameof(PlaceholderColor), source: this));

        EditorView.SetBinding(InputView.KeyboardProperty, new Binding(nameof(Keyboard), source: this));
        EditorView.SetBinding(InputView.CharacterSpacingProperty, new Binding(nameof(CharacterSpacing), source: this));
        EditorView.SetBinding(InputView.MaxLengthProperty, new Binding(nameof(MaxLength), source: this));
        EditorView.SetBinding(InputView.IsReadOnlyProperty, new Binding(nameof(IsReadOnly), source: this));
        EditorView.SetBinding(InputView.IsTextPredictionEnabledProperty, new Binding(nameof(IsTextPredictionEnabled), source: this));
        EditorView.SetBinding(InputView.IsSpellCheckEnabledProperty, new Binding(nameof(IsSpellCheckEnabled), source: this));

        EditorView.SetBinding(BorderlessEditor.CursorColorProperty, new Binding(nameof(CursorColor), source: this));
        EditorView.SetBinding(BorderlessEditor.AccentColorProperty, new Binding(nameof(AccentColor), source: this));

        EditorView.SetBinding(IsEnabledProperty, new Binding(nameof(IsEnabled), source: this));
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        InvalidateHeights();
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        propertyName ??= string.Empty;
        base.OnPropertyChanged(propertyName);

        if (propertyName is nameof(FontSize) or nameof(FontFamily))
            InvalidateHeights();
    }

    // Le Handler natif n'existe pas au parsing XAML : la 1re mesure retombe sur le fallback,
    // tout doit donc être recalculé dès qu'il devient disponible.
    private void InvalidateHeights()
    {
        _cachedLineHeight = null;
        ApplyHeights();
    }

    /// <summary>
    /// Passe unique : centre l'Editor sur <see cref="BaseHeightRequest"/> (hauteur à une ligne),
    /// puis en dérive les hauteurs min / max / fixe.
    /// </summary>
    private void ApplyHeights()
    {
        if (EditorView == null)
            return;

        _cachedLineHeight ??= MeasureLineHeight();
        double lineHeight = _cachedLineHeight.Value;

        // Marge = espace restant autour d'une ligne pour atteindre BaseHeightRequest.
        double margin = BaseHeightRequest > 0
            ? Math.Max(0, (BaseHeightRequest - lineHeight) / 2)
            : 0;

        EditorView.Margin = new Thickness(EditorView.Margin.Left, margin, EditorView.Margin.Right, margin);

        double verticalMargins = margin * 2;

        // Plancher = une ligne (== BaseHeightRequest) ; l'AutoSize de l'Editor fait grandir ensuite.
        MinimumHeightRequest = MinimumHeightByLines > 0
            ? (MinimumHeightByLines * lineHeight) + verticalMargins
            : lineHeight + verticalMargins;

        if (MaximumHeightByLines > 0)
            MaximumHeightRequest = (MaximumHeightByLines * lineHeight) + verticalMargins;

        if (HeightByLines > 0)
            HeightRequest = (HeightByLines * lineHeight) + verticalMargins;
    }

    /// <summary>
    /// Mesure la hauteur réelle d'une ligne de texte via les métriques natives de la police.
    /// Utilise <see cref="IFontManager"/> lorsque le Handler est disponible,
    /// sinon revient sur le fallback (<see cref="_fallbackLineHeightMultiplier"/>).
    /// </summary>
    private double MeasureLineHeight()
    {
        var services = Handler?.MauiContext?.Services;
        if (services == null)
            return FontSize * _fallbackLineHeightMultiplier;

        var fontManager = services.GetService<IFontManager>();
        if (fontManager == null)
            return FontSize * _fallbackLineHeightMultiplier;

        var mauiFont = string.IsNullOrEmpty(FontFamily)
            ? Microsoft.Maui.Font.SystemFontOfSize(FontSize)
            : Microsoft.Maui.Font.OfSize(FontFamily, FontSize);

#if ANDROID
        using var paint = new Android.Graphics.Paint();
        paint.SetTypeface(fontManager.GetTypeface(mauiFont));
        paint.TextSize = (float)FontSize;
        var fm = paint.GetFontMetrics();

        if (fm == null)
            return FontSize * _fallbackLineHeightMultiplier;

        return Math.Ceiling(-fm.Ascent + fm.Descent + fm.Leading);
#elif IOS
        var uiFont = fontManager.GetFont(mauiFont, FontSize);
        return uiFont.LineHeight;
#else
        return FontSize * _fallbackLineHeightMultiplier;
#endif
    }

    protected override object? GetValueForValidator() => Text;

    public override void ResetValidation()
    {
        Text = string.Empty;
        base.ResetValidation();
    }

    private void OnEditorCompleted(object? sender, EventArgs e)
        => Completed?.Invoke(this, e);

    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.OldTextValue) || string.IsNullOrEmpty(e.NewTextValue))
            UpdateState();

        if (e.NewTextValue is not null)
            CheckAndShowValidations();

        TextChanged?.Invoke(this, e);
    }

    public void ClearValue()
    {
        if (IsEnabled)
            Text = string.Empty;
    }
}
