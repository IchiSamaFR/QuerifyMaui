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
public class ButtonView : Border
{
    private Layout _layout;
    private bool _isPressed;
    private readonly Setter _hoverBackgroundSetter;

    /// <summary>Déclenché lors d'un tap complet (press + release sur le bouton).</summary>
    public event EventHandler? Clicked;

    /// <summary>Déclenché quand le bouton commence à être pressé.</summary>
    public event EventHandler? Pressed;

    /// <summary>Déclenché quand le doigt/curseur est relâché.</summary>
    public event EventHandler? Released;

    public ButtonView()
    {
        Padding = 0;
        StrokeThickness = 0;
        Stroke = Colors.Transparent;
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) };

        // Use SetDynamicResource so the color is resolved lazily from the app
        // ResourceDictionary, avoiding resource access before the app is loaded.
        this.SetDynamicResource(BackgroundColorProperty, "Background");

        _layout = new Grid();
        base.Content = _layout;

        AddTap();
        AddPointer();

        _hoverBackgroundSetter = new Setter
        {
            Property = BackgroundProperty,
            Value = HoverBackgroundColor
        };

        var visualStateGroups = new VisualStateGroupList
        {
            new VisualStateGroup
            {
                Name = "CommonStates",
                States =
                {
                    new VisualState {
                        Name = "Normal" },
                    new VisualState
                    {
                        Name = "PointerOver",
                        Setters = {
                            _hoverBackgroundSetter
                        }
                    }
                }
            }
        };

        VisualStateManager.SetVisualStateGroups(this, visualStateGroups);
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

            _layout = value;
            base.Content = _layout;
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

        VisualStateManager.GoToState(this, hovered ? "PointerOver" : "Normal");
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsEnabledProperty.PropertyName)
            Opacity = IsEnabled ? 1 : DisabledOpacity;
    }

    // ── Bindable Properties ──────────────────────────────────────────────

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.Create(
        nameof(Command), typeof(ICommand), typeof(ButtonView), null);

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
        nameof(CommandParameter), typeof(object), typeof(ButtonView), null);

    /// <summary>Opacité appliquée pendant l'état pressé.</summary>
    public double PressedOpacity
    {
        get => (double)GetValue(PressedOpacityProperty);
        set => SetValue(PressedOpacityProperty, value);
    }

    public static readonly BindableProperty PressedOpacityProperty = BindableProperty.Create(
        nameof(PressedOpacity), typeof(double), typeof(ButtonView), 0.85d);

    /// <summary>Opacité appliquée quand IsEnabled est false.</summary>
    public double DisabledOpacity
    {
        get => (double)GetValue(DisabledOpacityProperty);
        set => SetValue(DisabledOpacityProperty, value);
    }

    public static readonly BindableProperty DisabledOpacityProperty = BindableProperty.Create(
        nameof(DisabledOpacity), typeof(double), typeof(ButtonView), 0.6d);

    /// <summary>Couleur de fond appliquée pendant le survol (état PointerOver).</summary>
    public Color? HoverBackgroundColor
    {
        get => (Color?)GetValue(HoverBackgroundColorProperty);
        set => SetValue(HoverBackgroundColorProperty, value);
    }

    public static readonly BindableProperty HoverBackgroundColorProperty = BindableProperty.Create(
        nameof(HoverBackgroundColor), typeof(Color), typeof(ButtonView), null,
        propertyChanged: (bindable, _, newValue) =>
        {
            var btn = (ButtonView)bindable;
            if (btn._hoverBackgroundSetter is null)
                return;
            btn._hoverBackgroundSetter.Value = newValue ?? btn.BackgroundColor;
        });

    /// <summary>Raccourci pour le rayon des coins (met à jour StrokeShape).</summary>
    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(ButtonView), 8d,
        propertyChanged: (bindable, _, newValue) =>
            ((ButtonView)bindable).StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius((double)newValue)
            });

    /// <summary>Couleur de la bordure (raccourci pour Stroke).</summary>
    public Color? BorderColor
    {
        get => (Color?)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(ButtonView), Colors.Transparent,
        propertyChanged: (bindable, _, newValue) =>
        {
            var btn = (ButtonView)bindable;
            btn.Stroke = newValue is Color c ? c : Colors.Transparent;
        });

    /// <summary>Épaisseur de la bordure (raccourci pour StrokeThickness).</summary>
    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
        nameof(BorderThickness), typeof(double), typeof(ButtonView), 0d,
        propertyChanged: (bindable, _, newValue) =>
            ((ButtonView)bindable).StrokeThickness = (double)newValue);
}