using SqlToolManagerMaui.UI.Inputs.Converters;
using SqlToolManagerMaui.UI.Inputs.Core;
using SqlToolManagerMaui.UI.Inputs.Enums;
using System.Runtime.CompilerServices;

namespace SqlToolManagerMaui.UI.Inputs;


/// <summary>
/// Saisie de code PIN 100% MAUI (remplace PINView.Maui) : boîtes de caractères + Entry invisible
/// qui capte le clavier.
///
/// Dérive de <see cref="InputField"/> pour mutualiser le titre flottant, le message de validation,
/// les icônes et l'implémentation de <see cref="IValidatable"/> (donc AInputValidationBehavior).
/// Le Content est un conteneur (Entry cachée + boîtes), le focus réel est porté par l'Entry :
/// d'où la redéfinition de <see cref="FocusTarget"/>.
///
/// L'Entry cachée n'affiche ni caret ni menu contextuel : les couleurs transparentes ne suffisent
/// pas (Android redessine le caret, la bulle "Coller" survit à l'Unfocus), c'est
/// <see cref="BorderlessEntry.IsCaretVisible"/> / <see cref="BorderlessEntry.IsContextMenuEnabled"/>
/// qui les suppriment côté natif.
/// </summary>
public partial class PinInput : InputField
{
    private const char PasswordChar = '*';

    private readonly HorizontalStackLayout _boxesLayout = new HorizontalStackLayout
    {
        InputTransparent = true,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,
    };

    private readonly BorderlessEntry _hiddenEntry = new BorderlessEntry
    {
        Opacity = 0.01,
        BackgroundColor = Colors.Transparent,
        TextColor = Colors.Transparent,
        SelectionHighlightColor = Colors.Transparent,
        CursorColor = Colors.Transparent,
        IsCaretVisible = false,
        IsContextMenuEnabled = false,
        IsTextPredictionEnabled = false,
        IsSpellCheckEnabled = false,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill,
    };

    private readonly List<(Border Box, Label Label)> _boxes = new();
    private bool _syncing;

    public event EventHandler? Completed;

    public override View Content { get; set; }

    public override bool HasValue => !string.IsNullOrEmpty(Value);

    protected override View FocusTarget => _hiddenEntry;

    // Les boîtes portent déjà leur propre marge : pas d'inset supplémentaire pour le titre.
    protected override double ContentTextStartInset => 0;

    public PinInput()
    {
        var container = new Grid { HorizontalOptions = LayoutOptions.Fill };
        container.Add(_hiddenEntry);
        container.Add(_boxesLayout);
        Content = container;

        RegisterForEvents();

        _hiddenEntry.TextChanged += OnEntryTextChanged;

        ApplyKeyboard();
        BuildBoxes();
    }

    public new bool Focus() => _hiddenEntry.Focus();

    public new void Unfocus() => _hiddenEntry.Unfocus();

    protected override object? GetValueForValidator() => Value;

    private void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncing)
            return;
        _syncing = true;

        var text = Sanitize(e.NewTextValue ?? string.Empty);
        _hiddenEntry.Text = text;
        // Le caret est masqué mais reste positionné : on le garde en fin de texte pour que la
        // saisie continue au bon endroit même si l'utilisateur tape dans la zone.
        _hiddenEntry.CursorPosition = text.Length;
        Value = text;

        _syncing = false;

        UpdateState();

        if (text.Length == Length && Length > 0)
        {
            // L'Unfocus déclenche CheckAndShowValidations() côté InputField.
            _hiddenEntry.Unfocus();
            Completed?.Invoke(this, EventArgs.Empty);
            var parameter = CompletedCommandParameter ?? Value;
            if (CompletedCommand?.CanExecute(parameter) == true)
                CompletedCommand.Execute(parameter);
        }
    }

    private void OnValueChanged(string value)
    {
        if (_syncing)
            return;
        _syncing = true;
        var text = Sanitize(value);
        _hiddenEntry.Text = text;
        _hiddenEntry.CursorPosition = text.Length;
        _syncing = false;
        UpdateState();
    }

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(AccentColor):
            case nameof(ErrorColor):
            case nameof(BorderColor):
            case nameof(BorderThickness):
                UpdateBoxes();
                break;
            default:
                break;
        }
    }

    private string Sanitize(string text)
    {
        if (KeyboardType == PinInputKeyboardType.Numeric)
            text = new string(text.Where(char.IsDigit).ToArray());
        else
            text = new string(text.Where(char.IsLetterOrDigit).ToArray());

        if (KeyboardType == PinInputKeyboardType.AlphaNumericUppercase)
            text = text.ToUpperInvariant();

        return text.Length > Length ? text[..Length] : text;
    }

    private void ApplyKeyboard()
    {
        _hiddenEntry.MaxLength = Math.Max(Length, 0);
        _hiddenEntry.Keyboard = KeyboardType switch
        {
            PinInputKeyboardType.Numeric => Keyboard.Numeric,
            PinInputKeyboardType.AlphaNumericUppercase => Keyboard.Create(KeyboardFlags.CapitalizeCharacter),
            _ => Keyboard.Plain,
        };
    }

    // ── Rendu des boîtes ──────────────────────────────────────────────────────
    protected override void SetInputContentBindings()
    {
        // La bordure d'InputField reste invisible : chaque boîte porte son propre style.
        border.StrokeThickness = 0;
        border.BackgroundColor = Colors.Transparent;

        _boxesLayout.SetBinding(HorizontalStackLayout.SpacingProperty, new Binding(nameof(BoxSpacing), source: this));
    }

    /// <summary>
    /// Branche les propriétés statiques (indépendantes de l'index/valeur) d'une boîte
    /// sur les propriétés bindables de <see cref="PinInput"/>. Les propriétés dépendant
    /// de l'état (focus/erreur/index courant) restent gérées par <see cref="UpdateBoxes"/>.
    /// </summary>
    private void SetBoxBindings(Border box, Label label)
    {
        box.SetBinding(Border.WidthRequestProperty, new Binding(nameof(Size), source: this));
        box.SetBinding(Border.HeightRequestProperty, new Binding(nameof(Size), source: this));
        box.SetBinding(Border.StrokeShapeProperty, new MultiBinding
        {
            Bindings =
            {
                new Binding(nameof(Shape), source: this),
                new Binding(nameof(Size), source: this),
                new Binding(nameof(BoxCornerRadius), source: this),
                new Binding(nameof(CornerRadius), source: this),
            },
            Converter = new BoxShapeMultiConverter(),
        });
        box.SetBinding(Border.BackgroundProperty, new MultiBinding
        {
            Bindings =
            {
                new Binding(nameof(InputBackground), source: this),
                new Binding(nameof(InputBackgroundColor), source: this),
            },
            Converter = new InputBackgroundMultiConverter(),
        });

        label.SetBinding(Label.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        label.SetBinding(Label.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
        label.SetBinding(Label.TextColorProperty, new Binding(nameof(TextColor), source: this));
    }

    private void BuildBoxes()
    {
        _boxesLayout.Clear();
        _boxes.Clear();

        for (int i = 0; i < Length; i++)
        {
            var label = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };

            var box = new Border
            {
                Content = label,
                Padding = 0,
            };
            SetBoxBindings(box, label);

            _boxes.Add((box, label));
            _boxesLayout.Add(box);
        }

        UpdateBoxes();
    }

    /// <summary>
    /// Reprend le contrat de <see cref="InputField.UpdateColors"/> (focus &gt; erreur &gt; normal),
    /// mais sur les boîtes plutôt que sur la bordure du champ.
    /// </summary>
    protected override void UpdateColors()
    {
        base.UpdateColors();
        UpdateBoxes();
    }

    private void UpdateBoxes()
    {
        if (_boxesLayout == null || _boxes.Count == 0)
            return;

        var value = Value ?? string.Empty;
        bool focused = IsFocused;
        bool hasError = validationLabel.IsVisible;

        for (int i = 0; i < _boxes.Count; i++)
        {
            var (box, label) = _boxes[i];

            label.Text = i < value.Length
                ? (IsPassword ? PasswordChar.ToString() : value[i].ToString())
                : string.Empty;

            bool isCurrent = focused && i == Math.Min(value.Length, _boxes.Count - 1);
            box.Stroke = isCurrent ? AccentColor : hasError ? ErrorColor : EffectiveBoxBorderColor;
            var thickness = EffectiveBoxBorderThickness;
            box.StrokeThickness = isCurrent ? thickness + 1 : thickness;
        }
    }
}
