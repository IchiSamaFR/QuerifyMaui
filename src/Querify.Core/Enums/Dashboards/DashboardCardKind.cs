namespace Querify.Core.Enums.Dashboards;

/// <summary>Rendu d'une carte du dashboard.</summary>
public enum DashboardCardKind
{
    /// <summary>Tableau des lignes de la requete.</summary>
    Grid,

    /// <summary>Une valeur mise en avant (premiere ligne).</summary>
    Value,

    BarChart,

    LineChart,

    PieChart,

    /// <summary>Texte libre, sans requete.</summary>
    Text,

    /// <summary>Bouton qui ouvre une autre page.</summary>
    Button,
}
