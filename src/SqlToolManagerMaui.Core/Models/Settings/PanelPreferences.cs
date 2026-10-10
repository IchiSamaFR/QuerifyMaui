using SqlToolManagerMaui.Core.Enums.Settings;

namespace SqlToolManagerMaui.Core.Models.Settings;

/// <summary>Etat ouvert / ferme des volets, conserve d'une session a l'autre.</summary>
public sealed class PanelPreferences
{
    /// <summary>Seuls les volets deja bascules y figurent ; les autres restent ouverts.</summary>
    public Dictionary<PanelKind, bool> Visibility { get; set; } = [];

    public bool IsOpen(PanelKind panel) => !Visibility.TryGetValue(panel, out var isOpen) || isOpen;

    public void SetOpen(PanelKind panel, bool isOpen) => Visibility[panel] = isOpen;

    public PanelPreferences Clone() => new() { Visibility = new(Visibility) };
}
