namespace Querify.Core.Models.Dashboards;

/// <summary>Valeur d'un parametre a l'ouverture d'une page : colonne de la ligne cliquee, ou valeur (@Param = autre parametre).</summary>
public sealed class DashboardParameterMapping
{
    public required string Parameter { get; set; }

    public string? Column { get; set; }

    public string? Value { get; set; }
}
