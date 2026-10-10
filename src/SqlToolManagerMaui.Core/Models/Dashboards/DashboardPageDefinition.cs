namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>Page (sous-partie) du dashboard : une grille de cartes.</summary>
public sealed class DashboardPageDefinition
{
    /// <summary>Identifiant utilise par la navigation (unique dans le fichier).</summary>
    public required string Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<DashboardCardDefinition> Cards { get; init; } = [];
}
