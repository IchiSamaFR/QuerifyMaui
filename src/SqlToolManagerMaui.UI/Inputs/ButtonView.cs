using Microsoft.Maui.Controls.Shapes;
using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Inputs;

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
    private Layout _layout = new Grid();
    private bool _isPressed;
    private readonly Grid _root;
    private readonly BoxView _hoverOverlay = new BoxView
    {
        InputTransparent = true,
        Opacity = 0,
    };
    private readonly RoundRectangle _shape = new RoundRectangle()
    {
        CornerRadius = 14
    };

    /// <summary>Déclenché lors d'un tap complet (press + release sur le bouton).</summary>
    public event EventHandler? Clicked;

    /// <summary>Déclenché quand le bouton commence à être pressé.</summary>
    public event EventHandler? Pressed;

    /// <summary>Déclenché quand le doigt/curseur est relâché.</summary>
    public event EventHandler? Released;

    public ButtonView()
    {
        _shape.CornerRadius = new CornerRadius(CornerRadius);
        StrokeShape = _shape;

        _hoverOverlay.CornerRadius = new CornerRadius(CornerRadius);
        // La couleur du voile suit HoverOverlayColor ; seule son opacité est pilotée
        // par les états visuels, ce qui évite d'avoir à maintenir un Setter à la main.
        _hoverOverlay.SetBinding(BoxView.ColorProperty, new Binding(nameof(HoverOverlayColor), source: this));
        UpdateOverlayMargin();

        _root = new Grid { Children = { _layout, _hoverOverlay } };
        base.Content = _root;

        AddTap();
        AddPointer();

        BindVisualStateGroups();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        switch (propertyName)
        {
            case nameof(CornerRadius):
                _shape.CornerRadius = new CornerRadius(CornerRadius);
                _hoverOverlay.CornerRadius = new CornerRadius(CornerRadius);
                break;
            case nameof(IsEnabled):
                Opacity = IsEnabled ? 1 : DisabledOpacity;
                break;
            case nameof(Padding):
                UpdateOverlayMargin();
                break;
            default:
                break;
        }
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

    // ── Layout interne ──────────────────────────────────────────────

    /// <summary>
    /// Le layout qui héberge les enfants (Grid par défaut). Permet d'utiliser
    /// un HorizontalStackLayout, VerticalStackLayout, FlexLayout, etc.
    /// </summary>
    public Layout LayoutContent
    {
        get => _layout;
        set
        {
            if (value is null || ReferenceEquals(value, _layout))
                return;

            var existing = _layout.Children.ToList();
            _layout.Children.Clear();
            foreach (var child in existing)
                value.Children.Add(child);

            _root.Children[0] = value;
            _layout = value;
        }
    }

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

    /// <summary>
    /// Le Padding du Border s'applique à son contenu, donc aussi au voile. Une marge négative
    /// équivalente le fait ressortir jusqu'aux bords du bouton, où la StrokeShape le découpe.
    /// </summary>
    private void UpdateOverlayMargin()
    {
        if (_hoverOverlay is null)
            return;

        var padding = Padding;
        _hoverOverlay.Margin = new Thickness(-padding.Left, -padding.Top, -padding.Right, -padding.Bottom);
    }
}