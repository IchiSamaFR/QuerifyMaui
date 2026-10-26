using Querify.Core.Enums.Dashboards;
using Querify.Core.Enums.Editors.Highlighting;

namespace Querify.Core.Models.Dashboards;

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

    /// <summary>Langage de <see cref="Query"/> ; null = SQL (absent du fichier).</summary>
    public SyntaxLanguage? Language { get; set; }

    /// <summary>Requete SQL ou LINQ (EF) ; les parametres du dashboard s'y utilisent en @Nom.</summary>
    public string? Query { get; set; }

    /// <summary>Texte d'une carte Texte, ou libelle d'un bouton.</summary>
    public string? Text { get; set; }

    /// <summary>Graphique : colonne des libelles (axe X ou parts) ; a defaut, la premiere.</summary>
    public string? LabelColumn { get; set; }

    /// <summary>Graphique / valeur : colonnes numeriques affichees ; a defaut, toutes les autres.</summary>
    // set et non init : le generateur System.Text.Json remet a null une propriete init absente du JSON.
    public List<string> ValueColumns { get; set; } = [];

    /// <summary>Bouton, ou clic sur une ligne du tableau : page ouverte et parametres transmis.</summary>
    public DashboardNavigationDefinition? Navigation { get; set; }
}
