using Querify.UI.Converters;
using Querify.UI.Inputs.Converters;

namespace Querify.UI.Inputs;

/// <summary>
/// Bouton "container" : se comporte comme un Button (Command, Clicked, état pressé)
/// mais peut héberger n'importe quel layout et plusieurs enfants.
/// Réimplémentation standalone (sans UraniumUI), dans le style des inputs RaveOn.
///
/// Exemple :
/// <![CDATA[
///   <inp:ButtonView Command="{Binding SaveCommand}" CornerRadius="12">
///       <HorizontalStackLayout Spacing="8">
///           <Image Source="save.png" />
///           <Label Text="Enregistrer" />
///       </HorizontalStackLayout>
///   </inp:ButtonView>
/// ]]>
/// </summary>
[ContentProperty(nameof(Children))]
public partial class ButtonView : Border
{
    protected virtual Layout _layout { get; } = new Grid
    {
        VerticalOptions = LayoutOptions.Fill,
        HorizontalOptions = LayoutOptions.Fill,
    };
    private bool _isPressed;
    private readonly Grid _root;
    private readonly BoxView _hoverOverlay = new BoxView
    {
        VerticalOptions = LayoutOptions.Fill,
        HorizontalOptions = LayoutOptions.Fill,
        InputTransparent = true,
        Opacity = 0,
    };

    /// <summary>Déclenché lors d'un tap complet (press + release sur le bouton).</summary>
    public event EventHandler? Clicked;

    /// <summary>Déclenché quand le bouton commence à être pressé.</summary>
    public event EventHandler? Pressed;

    /// <summary>Déclenché quand le doigt/curseur est relâché.</summary>
    public event EventHandler? Released;

    public ButtonView()
    {
        _root = new Grid { Children = { _layout, _hoverOverlay } };
        base.Content = _root;

        AddTap();
        AddPointer();

        BindVisualStateGroups();
        RegisterBindings();
    }

    /// <summary>
    /// Branche les parties internes (bordure, voile de survol) sur les proprietes bindables
    /// du controle, plutot que de les resynchroniser a la main dans OnPropertyChanged.
    /// </summary>
    private void RegisterBindings()
    {
        // Bordure : les raccourcis CornerRadius / BorderColor / BorderThickness pilotent
        // la StrokeShape, le Stroke et son epaisseur.
        SetBinding(StrokeShapeProperty, new Binding(nameof(CornerRadius), source: this, converter: new CornerRadiusToStrokeShapeConverter()));
        SetBinding(StrokeProperty, new Binding(nameof(BorderColor), source: this, converter: new ColorToBrushConverter()));
        SetBinding(StrokeThicknessProperty, new Binding(nameof(BorderThickness), source: this));

        // Voile de survol : couleur et arrondi suivent le bouton ; seule son opacite est
        // pilotee par les etats visuels (voir BindVisualStateGroups).
        _hoverOverlay.SetBinding(BoxView.ColorProperty, new Binding(nameof(HoverOverlayColor), source: this));
        _hoverOverlay.SetBinding(BoxView.CornerRadiusProperty, new Binding(nameof(CornerRadius), source: this, converter: new DoubleToCornerRadiusConverter()));
        _hoverOverlay.SetBinding(MarginProperty, new Binding(nameof(Padding), source: this, converter: new PaddingToNegativeMarginConverter()));

        BindLayoutContent(_layout);
    }

    /// <summary>
    /// Branche l'alignement du contenu sur le layout interne. La TAILLE, elle, n'est jamais
    /// propagee : le controle EST le Border, une taille posee dessus contraint deja le
    /// contenu, et la repercuter ferait compter le Padding deux fois.
    /// </summary>
    private void BindLayoutContent(Layout layout)
    {
        layout.SetBinding(HorizontalOptionsProperty, new Binding(nameof(ContentHorizontalOptions), source: this));
        layout.SetBinding(VerticalOptionsProperty, new Binding(nameof(ContentVerticalOptions), source: this));
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        // Seule l'opacite reste imperative : elle combine IsEnabled ET l'etat presse,
        // qui n'est pas une propriete bindable.
        if (propertyName is nameof(IsEnabled))
            Opacity = IsEnabled ? 1 : DisabledOpacity;
    }

    private void BindVisualStateGroups()
    {
        var visualStateGroups = new VisualStateGroupList
        {
            new VisualStateGroup
            {
                Name = "HoverStates",
                States =
                {
                    new VisualState
                    {
                        Name = "Normal",
                        Setters =
                        {
                            new Setter { Property = OpacityProperty, Value = 0d },
                        }
                    },
                    new VisualState
                    {
                        Name = "PointerOver",
                        Setters =
                        {
                            new Setter { Property = OpacityProperty, Value = 1d },
                        }
                    }
                }
            }
        };

        // Les setters ciblent l'élément qui porte les groupes : le voile, pas le bouton.
        VisualStateManager.SetVisualStateGroups(_hoverOverlay, visualStateGroups);
    }

    /// <summary>Les enfants du layout interne. Cible de la ContentProperty (XAML).</summary>
    public IList<IView> Children => _layout.Children;

    // ── Comportement bouton ──────────────────────────────────────────────

    private void AddTap()
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OnTapped();
        GestureRecognizers.Add(tap);
    }

    private void AddPointer()
    {
        var pointer = new PointerGestureRecognizer();
        pointer.PointerPressed += (_, _) => SetPressed(true);
        pointer.PointerReleased += (_, _) => SetPressed(false);
        pointer.PointerEntered += (_, _) => SetHovered(true);
        pointer.PointerExited += (_, _) =>
        {
            SetPressed(false);
            SetHovered(false);
        };
        GestureRecognizers.Add(pointer);
    }

    private void OnTapped()
    {
        if (!IsEnabled)
            return;

        Clicked?.Invoke(this, EventArgs.Empty);

        if (Command?.CanExecute(CommandParameter) == true)
            Command.Execute(CommandParameter);
    }

    private void SetPressed(bool pressed)
    {
        if (_isPressed == pressed || !IsEnabled)
            return;

        _isPressed = pressed;

        if (pressed)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
            Opacity = PressedOpacity;
        }
        else
        {
            Released?.Invoke(this, EventArgs.Empty);
            Opacity = IsEnabled ? 1 : DisabledOpacity;
        }
    }

    private void SetHovered(bool hovered)
    {
        if (!IsEnabled)
            return;

        VisualStateManager.GoToState(_hoverOverlay, hovered ? "PointerOver" : "Normal");
    }
}