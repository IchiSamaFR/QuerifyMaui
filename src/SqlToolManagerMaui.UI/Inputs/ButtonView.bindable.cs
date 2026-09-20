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

    // Stroke suit BorderColor par binding (voir ButtonView.RegisterBindings).
    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor), typeof(Color), typeof(ButtonView), Colors.Transparent);

    /// <summary>Épaisseur de la bordure (raccourci pour StrokeThickness).</summary>
    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    // StrokeThickness suit BorderThickness par binding (voir ButtonView.RegisterBindings).
    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
        nameof(BorderThickness), typeof(double), typeof(ButtonView), 0d);

    /// <summary>
    /// Alignement horizontal du contenu dans le bouton (Fill par defaut : le layout
    /// interne occupe toute la largeur, comme aujourd'hui).
    /// </summary>
    public LayoutOptions ContentHorizontalOptions
    {
        get => (LayoutOptions)GetValue(ContentHorizontalOptionsProperty);
        set => SetValue(ContentHorizontalOptionsProperty, value);
    }

    public static readonly BindableProperty ContentHorizontalOptionsProperty = BindableProperty.Create(
        nameof(ContentHorizontalOptions), typeof(LayoutOptions), typeof(ButtonView), LayoutOptions.Center);

    /// <summary>Alignement vertical du contenu dans le bouton (Fill par defaut).</summary>
    public LayoutOptions ContentVerticalOptions
    {
        get => (LayoutOptions)GetValue(ContentVerticalOptionsProperty);
        set => SetValue(ContentVerticalOptionsProperty, value);
    }

    public static readonly BindableProperty ContentVerticalOptionsProperty = BindableProperty.Create(
        nameof(ContentVerticalOptions), typeof(LayoutOptions), typeof(ButtonView), LayoutOptions.Center);
}
