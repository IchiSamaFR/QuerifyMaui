namespace SqlToolManagerMaui.UI.Inputs.Core;

/// <summary>
/// Versions "nues" des inputs natifs (sans underline Android / bordure iOS),
/// destinées à être habillées par <see cref="InputField"/>.
/// Remplace Plainer.Maui.
/// </summary>
public class BorderlessEntry : Entry
{
    static BorderlessEntry() => BorderlessHandlers.Init();

    public static readonly BindableProperty SelectionHighlightColorProperty = BindableProperty.Create(
        nameof(SelectionHighlightColor),
        typeof(Color),
        typeof(BorderlessEntry),
        null,
        propertyChanged: (bindable, _, _) => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(SelectionHighlightColor)));

    public Color? SelectionHighlightColor
    {
        get => (Color?)GetValue(SelectionHighlightColorProperty);
        set => SetValue(SelectionHighlightColorProperty, value);
    }

    public static readonly BindableProperty CursorColorProperty = BindableProperty.Create(
        nameof(CursorColor),
        typeof(Color),
        typeof(BorderlessEntry),
        null,
        propertyChanged: (bindable, _, _) => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(CursorColor)));

    public Color? CursorColor
    {
        get => (Color?)GetValue(CursorColorProperty);
        set => SetValue(CursorColorProperty, value);
    }

    /// <summary>
    /// Affiche ou masque le caret natif. Contrairement à <see cref="CursorColor"/> (qui ne fait que
    /// le teinter et laisse iOS/Android le dessiner), false le supprime au niveau natif :
    /// indispensable pour une Entry cachée derrière un rendu custom (PinInput).
    /// </summary>
    public static readonly BindableProperty IsCaretVisibleProperty = BindableProperty.Create(
        nameof(IsCaretVisible),
        typeof(bool),
        typeof(BorderlessEntry),
        true,
        propertyChanged: (bindable, _, _) => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(IsCaretVisible)));

    public bool IsCaretVisible
    {
        get => (bool)GetValue(IsCaretVisibleProperty);
        set => SetValue(IsCaretVisibleProperty, value);
    }

    /// <summary>
    /// Autorise le menu contextuel natif (Couper / Copier / Coller, bulle "Coller" d'Android,
    /// UIMenu d'iOS) et les poignées de sélection. false empêche la bulle d'apparaître —
    /// et donc de rester affichée après un unfocus programmatique.
    /// </summary>
    public static readonly BindableProperty IsContextMenuEnabledProperty = BindableProperty.Create(
        nameof(IsContextMenuEnabled),
        typeof(bool),
        typeof(BorderlessEntry),
        true,
        propertyChanged: (bindable, _, _) => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(IsContextMenuEnabled)));

    public bool IsContextMenuEnabled
    {
        get => (bool)GetValue(IsContextMenuEnabledProperty);
        set => SetValue(IsContextMenuEnabledProperty, value);
    }

    /// <summary>
    /// Couleur d'accent du champ. Sur Windows, elle remplace l'accent systeme utilise par WinUI
    /// pour la bordure basse du TextBox quand il a le focus (bleu par defaut).
    /// </summary>
    public static readonly BindableProperty AccentColorProperty = BindableProperty.Create(
        nameof(AccentColor),
        typeof(Color),
        typeof(BorderlessEntry),
        null,
        propertyChanged: (bindable, _, _) => ((BorderlessEntry)bindable).Handler?.UpdateValue(nameof(AccentColor)));

    public Color? AccentColor
    {
        get => (Color?)GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }
}
