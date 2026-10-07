using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// Versions "nues" des inputs natifs (sans underline Android / bordure iOS),
/// destinées à être habillées par <see cref="InputField"/>.
/// Remplace Plainer.Maui.
/// </summary>
public partial class BorderlessEntry : Entry
{
    static BorderlessEntry() => BorderlessHandlers.Init();

    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectionHighlightColorChanged))]
    public partial Color? SelectionHighlightColor { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnCursorColorChanged))]
    public partial Color? CursorColor { get; set; }

    /// <summary>
    /// Affiche ou masque le caret natif. Contrairement à <see cref="CursorColor"/> (qui ne fait que
    /// le teinter et laisse iOS/Android le dessiner), false le supprime au niveau natif :
    /// indispensable pour une Entry cachée derrière un rendu custom (PinInput).
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnIsCaretVisibleChanged))]
    public partial bool IsCaretVisible { get; set; } = true;

    /// <summary>
    /// Autorise le menu contextuel natif (Couper / Copier / Coller, bulle "Coller" d'Android,
    /// UIMenu d'iOS) et les poignées de sélection. false empêche la bulle d'apparaître —
    /// et donc de rester affichée après un unfocus programmatique.
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnIsContextMenuEnabledChanged))]
    public partial bool IsContextMenuEnabled { get; set; } = true;

    /// <summary>
    /// Couleur d'accent du champ. Sur Windows, elle remplace l'accent systeme utilise par WinUI
    /// pour la bordure basse du TextBox quand il a le focus (bleu par defaut).
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnAccentColorChanged))]
    public partial Color? AccentColor { get; set; }

    private static void OnSelectionHighlightColorChanged(BindableObject bindable, object oldValue, object newValue)
        => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(SelectionHighlightColor));

    private static void OnCursorColorChanged(BindableObject bindable, object oldValue, object newValue)
        => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(CursorColor));

    private static void OnIsCaretVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(IsCaretVisible));

    private static void OnIsContextMenuEnabledChanged(BindableObject bindable, object oldValue, object newValue)
        => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(IsContextMenuEnabled));

    private static void OnAccentColorChanged(BindableObject bindable, object oldValue, object newValue)
        => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(AccentColor));
}
