namespace SqlToolManagerMaui.Core.Models.Settings;

/// <summary>Onglets ouverts a la fermeture de l'application, rouverts tels quels au demarrage suivant.</summary>
public sealed class WorkspaceState
{
    /// <summary>Onglets, dans l'ordre de la barre.</summary>
    public List<WorkspaceTab> Tabs { get; set; } = [];

    /// <summary>Compteurs des « Sans titre N » / « Dashboard N » : les nouveaux documents ne reprennent pas un nom deja pris.</summary>
    public int UntitledCount { get; set; }

    public int UntitledDashboardCount { get; set; }
}
