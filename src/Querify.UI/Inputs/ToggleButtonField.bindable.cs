using CommunityToolkit.Maui;
using System.ComponentModel;

namespace Querify.UI.Inputs;

public partial class ToggleButtonField
{
    // ── Bindable Properties ──────────────────────────────────────────────

    /// <summary>Etat courant, inverse a chaque clic.</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnIsToggledPropertyChanged))]
    public partial bool IsToggled { get; set; }

    /// <summary>
    /// Valeur courante, alternative a <see cref="IsToggled"/> : vaut <see cref="ToggledOnValue"/> ou
    /// <see cref="ToggledOffValue"/> selon l'etat. Une valeur egale a aucune des deux laisse le controle a l'etat faux.
    /// </summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnToggledValuePropertyChanged))]
    public partial object? ToggledValue { get; set; }

    /// <summary>Valeur de <see cref="ToggledValue"/> quand <see cref="IsToggled"/> est vrai.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnToggledValuePropertyChanged))]
    public partial object? ToggledOnValue { get; set; }

    /// <summary>Valeur de <see cref="ToggledValue"/> quand <see cref="IsToggled"/> est faux.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnToggledValuePropertyChanged))]
    public partial object? ToggledOffValue { get; set; }

    [BindableProperty]
    public partial ImageSource? OffIcon { get; set; }

    [BindableProperty]
    public partial ImageSource? OnIcon { get; set; }

    /// <summary>Libelle affiche quand <see cref="IsToggled"/> est faux ; vide = pas de libelle.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnStatePropertyChanged))]
    public partial string? OffText { get; set; }

    /// <summary>Libelle affiche quand <see cref="IsToggled"/> est vrai ; vide = pas de libelle.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnStatePropertyChanged))]
    public partial string? OnText { get; set; }

    /// <summary>Opacite de l'icone de l'etat inactif.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnStatePropertyChanged))]
    public partial double InactiveIconOpacity { get; set; } = 0.4d;

    [BindableProperty]
    public partial double IconSize { get; set; } = 16d;

    [TypeConverter(typeof(FontSizeConverter))]
    [BindableProperty]
    public partial double FontSize { get; set; } = 12d;

    /// <summary>Largeur fixe du libelle : evite que le controle change de taille a chaque bascule.</summary>
    [BindableProperty]
    public partial double LabelWidthRequest { get; set; } = -1d;

    private static void OnStatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((ToggleButtonField)bindable).UpdateState();

    private static void OnIsToggledPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((ToggleButtonField)bindable).OnIsToggledChanged();

    private static void OnToggledValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((ToggleButtonField)bindable).SyncIsToggledFromValue();
}
