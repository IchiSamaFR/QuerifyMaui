using SqlToolManagerMaui.Core.Enums.Dashboards;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>Carte placee sur la grille d'une page (colonnes et lignes en base 0).</summary>
public sealed class DashboardCardDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    public string? Title { get; set; }

    public int Column { get; set; }

    public int Row { get; set; }

    public int ColumnSpan { get; set; } = 4;

    public int RowSpan { get; set; } = 2;

    public DashboardCardKind View { get; set; } = DashboardCardKind.Grid;

    /// <summary>Requete SQL ; les parametres du dashboard s'y utilisent en @Nom.</summary>
    public string? Query { get; set; }

    /// <summary>Texte d'une carte Texte, ou libelle d'un bouton.</summary>
    public string? Text { get; set; }

    /// <summary>Graphique : colonne des libelles (axe X ou parts) ; a defaut, la premiere.</summary>
    public string? LabelColumn { get; set; }

    /// <summary>Graphique / valeur : colonnes numeriques affichees ; a defaut, toutes les autres.</summary>
    public List<string> ValueColumns { get; init; } = [];

    /// <summary>Bouton, ou clic sur une ligne du tableau : page ouverte et parametres transmis.</summary>
    public DashboardNavigationDefinition? Navigation { get; set; }
}
