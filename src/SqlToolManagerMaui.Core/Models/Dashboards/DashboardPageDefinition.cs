namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>Page (sous-partie) du dashboard : une grille de cartes.</summary>
public sealed class DashboardPageDefinition
{
    /// <summary>Identifiant utilise par la navigation (unique dans le fichier).</summary>
    public required string Id { get; set; }

    public string Title { get; set; } = string.Empty;

    // set et non init : le generateur System.Text.Json remet a null une propriete init absente du JSON.
    public List<DashboardCardDefinition> Cards { get; set; } = [];
}
