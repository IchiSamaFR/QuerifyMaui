using CommunityToolkit.Maui;
using System.Runtime.CompilerServices;

namespace Querify.UI.Inputs;

/// <summary>
/// Cadre d'un champ de saisie : titre, bordure (accent au focus, couleur d'erreur) et message d'erreur,
/// autour d'un contrôle natif (<see cref="TextInput"/>, <see cref="SelectInput"/>…) laissé intact.
/// </summary>
public partial class FormField : ContentView
{
    private VisualElement? _focusTarget;

    /// <summary>Titre affiché au-dessus du champ ; vide = masqué.</summary>
    [BindableProperty]
    public partial string? Title { get; set; }

    /// <summary>Message d'erreur affiché sous le champ ; non vide = bordure en <see cref="ErrorColor"/>.</summary>
    [BindableProperty]
    public partial string? ErrorText { get; set; }

    [BindableProperty]
    public partial double TitleFontSize { get; set; } = 12d;

    [BindableProperty]
    public partial Color? TitleColor { get; set; }

    [BindableProperty]
    public partial Color? BorderColor { get; set; } = Colors.Gray;

    [BindableProperty]
    public partial double BorderThickness { get; set; } = 1d;

    /// <summary>Couleur de la bordure quand le contrôle a le focus.</summary>
    [BindableProperty]
    public partial Color? AccentColor { get; set; } = Colors.DodgerBlue;

    [BindableProperty]
    public partial Color? ErrorColor { get; set; } = Colors.Red;

    [BindableProperty]
    public partial Color? InputBackgroundColor { get; set; }

    [BindableProperty]
    public partial double CornerRadius { get; set; } = 8d;

    /// <summary>Hauteur de la zone de saisie (bordure comprise) ; -1 = hauteur du contenu.</summary>
    [BindableProperty]
    public partial double InputHeight { get; set; } = -1d;

    /// <summary>Vrai tant que le contrôle contenu a le focus.</summary>
    [BindableProperty]
    public partial bool IsContentFocused { get; private set; }

    public FormField()
    {
        InitializeComponent();
    }

    // Le focus est porté par le contenu, connu seulement à l'exécution : suivi par événements.
    protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == ContentProperty.PropertyName)
            TrackFocus(Content);
    }

    private void TrackFocus(VisualElement? target)
    {
        if (_focusTarget is not null)
        {
            _focusTarget.Focused -= OnContentFocusChanged;
            _focusTarget.Unfocused -= OnContentFocusChanged;
        }

        _focusTarget = target;
        IsContentFocused = target?.IsFocused == true;

        if (target is not null)
        {
            target.Focused += OnContentFocusChanged;
            target.Unfocused += OnContentFocusChanged;
        }
    }

    private void OnContentFocusChanged(object? sender, FocusEventArgs e) => IsContentFocused = e.IsFocused;
}
