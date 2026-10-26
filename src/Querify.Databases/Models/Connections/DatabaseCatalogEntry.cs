namespace Querify.Databases.Models.Connections;

/// <summary>Base de donnees d'un serveur.</summary>
/// <param name="Name">Nom de la base.</param>
/// <param name="IsSystem">Vrai pour master, model, msdb, tempdb.</param>
public sealed record DatabaseCatalogEntry(string Name, bool IsSystem);

/// <summary>Objet d'une base (table, vue, procedure), qualifie par son schema.</summary>
/// <param name="Comment">Commentaire de l'objet (MS_Description sous SQL Server), null si absent.</param>
public sealed record DatabaseObjectEntry(string Schema, string Name, string? Comment = null)
{
    public string QualifiedName => $"{Schema}.{Name}";
}
