using CommunityToolkit.Maui;
using System.Windows.Input;

namespace Querify.UI.Inputs;

public partial class ButtonView
{
    // ── Bindable Properties ──────────────────────────────────────────────

    [BindableProperty]
    public partial ICommand? Command { get; set; }

    [BindableProperty]
    public partial object? CommandParameter { get; set; }

    /// <summary>Opacité appliquée pendant l'état pressé.</summary>
    [BindableProperty]
    public partial double PressedOpacity { get; set; } = 0.85d;

    /// <summary>Opacité appliquée quand IsEnabled est false.</summary>
    [BindableProperty]
    public partial double DisabledOpacity { get; set; } = 0.6d;

    /// <summary>
    /// Couleur du voile affiché pendant le survol, transparence comprise.
    /// Par défaut blanc à 10 % d'opacité (soit transparent à 90 %).
    /// </summary>
    [BindableProperty]
    public partial Color HoverOverlayColor { get; set; } = Colors.White.WithAlpha(0.1f);

    /// <summary>Raccourci pour le rayon des coins (met à jour StrokeShape).</summary>
    [BindableProperty]
    public partial double CornerRadius { get; set; } = 8d;

    // Stroke suit BorderColor par binding (voir ButtonView.RegisterBindings).
    /// <summary>Couleur de la bordure (raccourci pour Stroke).</summary>
    [BindableProperty]
    public partial Color? BorderColor { get; set; } = Colors.Transparent;

    // StrokeThickness suit BorderThickness par binding (voir ButtonView.RegisterBindings).
    /// <summary>Épaisseur de la bordure (raccourci pour StrokeThickness).</summary>
    [BindableProperty]
    public partial double BorderThickness { get; set; }

    /// <summary>
    /// Alignement horizontal du contenu dans le bouton (Fill par defaut : le layout
    /// interne occupe toute la largeur, comme aujourd'hui).
    /// </summary>
    [BindableProperty]
    public partial LayoutOptions ContentHorizontalOptions { get; set; } = LayoutOptions.Center;

    /// <summary>Alignement vertical du contenu dans le bouton (Fill par defaut).</summary>
    [BindableProperty]
    public partial LayoutOptions ContentVerticalOptions { get; set; } = LayoutOptions.Center;
}
