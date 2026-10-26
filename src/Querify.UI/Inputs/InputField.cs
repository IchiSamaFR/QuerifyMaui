using Microsoft.Maui.Controls.Shapes;
using Querify.UI.Extensions;
using Querify.UI.Inputs.Converters;
using Querify.UI.Inputs.Core;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Querify.UI.Inputs;

/// <summary>
/// Base "Material outlined" 100% MAUI (sans UraniumUI) pour les champs de saisie :
/// bordure arrondie, titre flottant animé, icônes de début/fin, attachments et validations.
///
/// Layout :
///   Row 0 — espace réservé au titre flottant (au-dessus de la bordure, plus de Margin hack)
///   Row 1 — la bordure contenant [icônes | contenu | icônes de fin]
///   Row 2 — le message de validation
/// </summary>
[ContentProperty(nameof(Validations))]
public abstract partial class InputField : Grid, IValidatable
{
    protected const double TitleGap = 2;

    private double FloatedTitleScale => TitleFontSize > 0 ? FloatedTitleFontSize / TitleFontSize : 0.75;

    protected readonly Border border = new Border
    {
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
        StrokeThickness = 1,
        Stroke = Colors.Gray,
        Background = null,
        Padding = 0
    };
    protected readonly Grid contentGrid = new Grid
    {
        ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
        ColumnSpacing = 0,
    };
    protected readonly HorizontalStackLayout startIconsContainer = new HorizontalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
    protected readonly HorizontalStackLayout endIconsContainer = new HorizontalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center };
    protected readonly Label titleLabel = new Label
    {
        HorizontalOptions = LayoutOptions.Start,
        VerticalOptions = LayoutOptions.Start,
        InputTransparent = true,
        AnchorX = 0,
        AnchorY = 0,
        LineBreakMode = LineBreakMode.NoWrap,
        IsVisible = false,
    };
    protected readonly Label validationLabel = new Label
    {
        FontSize = 12,
        TextColor = Colors.Red,
        Margin = new Thickness(12, TitleGap, 0, 0),
        IsVisible = false,
    };

    private readonly RowDefinition _spacerRow = new RowDefinition { Height = new GridLength(0) };
    private bool _isContentFocused;
    private bool _contentRegistered;
    private static readonly string[] _inputFieldIconStyleClass = new[] { "InputField.Icon" };

    protected Lazy<Image> imageIcon = new(() => new Image
    {
        StyleClass = _inputFieldIconStyleClass,
        HorizontalOptions = LayoutOptions.Start,
        VerticalOptions = LayoutOptions.Center,
        WidthRequest = 20,
        HeightRequest = 20,
        Margin = new Thickness(8, 0, 0, 0),
    });

    /// <summary>Le contrôle de saisie interne (Entry, Editor, pickers...).</summary>
    public abstract View Content { get; set; }

    /// <summary>
    /// Le contrôle qui porte réellement le focus. Par défaut <see cref="Content"/> ; les champs dont
    /// le Content est un conteneur (une Entry cachée derrière un rendu custom) le redéfinissent.
    /// </summary>
    protected virtual View FocusTarget => Content;

    /// <summary>Indique si le champ a une valeur (pilote le titre flottant).</summary>
    public abstract bool HasValue { get; }

    /// <summary>Vues additionnelles ajoutées en fin de champ (ex : œil show/hide password).</summary>
    public IList<View> Attachments { get; } = new ObservableCollection<View>();

    public IList<IFieldValidation> Validations { get; } = new List<IFieldValidation>();

    public bool IsValid => Validations.All(v => v.Validate(GetValueForValidator()));

    public new bool IsFocused => _isContentFocused;

    protected InputField()
    {
        ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star) };
        RowDefinitions = new RowDefinitionCollection
        {
            _spacerRow,
            new RowDefinition { Height = GridLength.Auto },
            new RowDefinition { Height = GridLength.Auto },
        };
        RowSpacing = 0;

        contentGrid.Add(startIconsContainer, column: 0);
        contentGrid.Add(endIconsContainer, column: 2);

        border.Content = contentGrid;
        this.Add(border, column: 0, row: 1);

        this.Add(titleLabel, column: 0, row: 0);
        Grid.SetRowSpan(titleLabel, 2);

        this.Add(validationLabel, column: 0, row: 2);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (IsEnabled && FocusTarget is { IsFocused: false } target)
                target.Focus();
        };
        border.GestureRecognizers.Add(tap);

        ((ObservableCollection<View>)Attachments).CollectionChanged += OnAttachmentsChanged;

        border.SizeChanged += (_, _) => UpdateTitlePosition(animate: false);
        titleLabel.SizeChanged += (_, _) =>
        {
            UpdateSpacerHeight();
            UpdateTitlePosition(animate: false);
        };
        startIconsContainer.SizeChanged += (_, _) => UpdateTitlePosition(animate: false);

        Loaded += (_, _) => UpdateState(animate: false);

        RegisterBindings();
    }

    /// <summary>
    /// À appeler dans le constructeur du contrôle dérivé, une fois <see cref="Content"/> affecté :
    /// insère le contenu dans la bordure et branche le focus.
    /// </summary>
    protected void RegisterForEvents()
    {
        if (_contentRegistered || Content is null)
            return;
        _contentRegistered = true;

        contentGrid.Add(Content, column: 1);
        FocusTarget.Focused += OnContentFocused;
        FocusTarget.Unfocused += OnContentUnfocused;
    }

    private void RegisterBindings()
    {
        titleLabel.SetBinding(Label.TextProperty, new Binding(nameof(Title), source: this));
        titleLabel.SetBinding(Label.FontSizeProperty, new Binding(nameof(TitleFontSize), source: this));
        titleLabel.SetBinding(Label.TextColorProperty, new Binding(nameof(TitleColor), source: this));
        titleLabel.SetBinding(Label.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));

        validationLabel.SetBinding(Label.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));

        SetInputContentBindings();
    }

    protected virtual void SetInputContentBindings()
    {
        border.SetBinding(Border.HeightRequestProperty, new Binding(nameof(HeightRequest), source: this));
        border.SetBinding(Border.MaximumHeightRequestProperty, new Binding(nameof(MaximumHeightRequest), source: this));
        border.SetBinding(Border.MinimumHeightRequestProperty, new Binding(nameof(MinimumHeightRequest), source: this));
        border.SetBinding(Border.VerticalOptionsProperty, new Binding(nameof(VerticalOptions), source: this));
        border.SetBinding(Border.HorizontalOptionsProperty, new Binding(nameof(HorizontalOptions), source: this));
        border.SetBinding(Border.StrokeProperty, new Binding(nameof(BorderColor), source: this));
        border.SetBinding(Border.StrokeThicknessProperty, new Binding(nameof(BorderThickness), source: this));
        border.SetBinding(Border.OpacityProperty, new Binding(nameof(IsEnabled), source: this, converter: new BoolToOpacityConverter()));
        border.SetBinding(Border.StrokeShapeProperty, new Binding(nameof(CornerRadius), source: this, converter: new CornerRadiusToStrokeShapeConverter()));
        border.SetBinding(Border.BackgroundProperty, new MultiBinding
        {
            Bindings =
            {
                new Binding(nameof(InputBackground), source: this),
                new Binding(nameof(InputBackgroundColor), source: this),
            },
            Converter = new InputBackgroundMultiConverter(),
        });
    }

    private void OnContentFocused(object? sender, FocusEventArgs e)
    {
        _isContentFocused = true;
        OnPropertyChanged(nameof(IsFocused));
        UpdateState();
    }

    private void OnContentUnfocused(object? sender, FocusEventArgs e)
    {
        _isContentFocused = false;
        OnPropertyChanged(nameof(IsFocused));
        CheckAndShowValidations();
        UpdateState();
    }

    private void OnAttachmentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (View view in e.OldItems)
                endIconsContainer.Remove(view);
        }

        if (e.NewItems is not null)
        {
            foreach (View view in e.NewItems)
                endIconsContainer.Add(view);
        }
    }

    // ── Titre flottant ────────────────────────────────────────────────────────

    /// <summary>Décalage horizontal du texte du contenu (pour aligner le titre).</summary>
    protected virtual double ContentTextStartInset => 16;

    protected virtual bool IsFloated => _isContentFocused || HasValue;

    private double MeasuredTitleHeight
        => titleLabel.Height > 0 ? titleLabel.Height : TitleFontSize * 1.4;

    private void UpdateSpacerHeight()
    {
        double height = string.IsNullOrEmpty(Title)
            ? 0
            : MeasuredTitleHeight * FloatedTitleScale + TitleGap;

        if (Math.Abs(_spacerRow.Height.Value - height) > 0.5)
            _spacerRow.Height = new GridLength(height);
    }

    protected void UpdateState(bool animate = true)
    {
        UpdateTitlePosition(animate);
        UpdateColors();
    }

    private void UpdateTitlePosition(bool animate)
    {
        if (string.IsNullOrEmpty(Title))
            return;

        double targetY = 0;
        double targetX = 0;
        double targetScale = 1;

        if (IsFloated || IsFloatedTitleFixed)
        {
            targetScale = FloatedTitleFontSize / TitleFontSize;
        }
        else
        {
            double titleHeight = MeasuredTitleHeight;

            targetY = _spacerRow.Height.Value + border.StrokeThickness + (border.Height - Content.Height) / 2;
            targetX = startIconsContainer.Width + border.StrokeThickness + Content.Margin.Left;
        }

        Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(titleLabel);

        if (animate && IsLoaded)
        {
            titleLabel.TranslateToAsync(targetX, targetY, 150, Easing.SinOut).FireAndForget();
            titleLabel.ScaleToAsync(targetScale, 150, Easing.SinOut).FireAndForget();
        }
        else
        {
            titleLabel.TranslationX = targetX;
            titleLabel.TranslationY = targetY;
            titleLabel.Scale = targetScale;
        }
    }

    protected virtual void UpdateColors()
    {
        bool hasError = validationLabel.IsVisible;

        if (IsFocused)
        {
            border.Stroke = AccentColor;
            titleLabel.TextColor = AccentColor;
        }
        else if (hasError)
        {
            border.Stroke = ErrorColor;
            titleLabel.TextColor = ErrorColor;
        }
        else
        {
            border.Stroke = BorderColor;
            titleLabel.TextColor = TitleColor;
        }

        validationLabel.TextColor = ErrorColor;
    }

    // ── Validations ───────────────────────────────────────────────────────────

    protected abstract object? GetValueForValidator();

    protected void CheckAndShowValidations() => DisplayValidation();

    public void DisplayValidation()
    {
        var value = GetValueForValidator();
        var firstError = Validations.FirstOrDefault(v => !v.Validate(value));

        validationLabel.Text = firstError?.Message ?? string.Empty;
        validationLabel.IsVisible = firstError is not null;
        UpdateColors();
    }

    public virtual void ResetValidation()
    {
        validationLabel.Text = string.Empty;
        validationLabel.IsVisible = false;
        UpdateColors();
    }

    // ── Icône de début ────────────────────────────────────────────────────────

    protected virtual void OnIconChanged()
    {
        if (Icon is null)
        {
            if (imageIcon.IsValueCreated)
                startIconsContainer.Remove(imageIcon.Value);

            imageIcon.Value.SetBinding(Label.HeightProperty, new Binding(nameof(IconSizeRequest), source: this));
            imageIcon.Value.SetBinding(Label.WidthProperty, new Binding(nameof(IconSizeRequest), source: this));
            return;
        }

        imageIcon.Value.Source = Icon;

        if (Icon is FontImageSource font && (font.Color is null || font.Color == Colors.Transparent))
        {
            font.SetAppThemeColor(
                FontImageSource.ColorProperty,
                InputColors.Get("OnBackground", Colors.Gray),
                InputColors.Get("OnBackgroundDark", Colors.Gray));
        }

        if (!startIconsContainer.Contains(imageIcon.Value))
            startIconsContainer.Add(imageIcon.Value);
    }

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(IsEnabled):
                titleLabel.Opacity = IsEnabled ? 1 : 0.6;
                break;
            case nameof(HeightRequest):
                UpdateTitlePosition(animate: false);
                break;
            default:
                break;
        }
    }
}
