using System.Windows.Input;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class ButtonView
{
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

    /// <summary>
    /// Couleur du voile affiché pendant le survol, transparence comprise.
    /// Par défaut blanc à 10 % d'opacité (soit transparent à 90 %).
    /// </summary>
    public Color HoverOverlayColor
    {
        get => (Color)GetValue(HoverOverlayColorProperty);
        set => SetValue(HoverOverlayColorProperty, value);
    }

    public static readonly BindableProperty HoverOverlayColorProperty = BindableProperty.Create(
        nameof(HoverOverlayColor), typeof(Color), typeof(ButtonView), Colors.White.WithAlpha(0.1f));

    /// <summary>Raccourci pour le rayon des coins (met à jour StrokeShape).</summary>
    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(ButtonView), 8d);

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