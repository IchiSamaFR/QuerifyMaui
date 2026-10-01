namespace SqlToolManagerMaui.Models.Databases;

/// <summary>Base de donnees d'un serveur.</summary>
/// <param name="Name">Nom de la base.</param>
/// <param name="IsSystem">Vrai pour master, model, msdb, tempdb.</param>
public sealed record DatabaseCatalogEntry(string Name, bool IsSystem);

/// <summary>Objet d'une base (table, vue, procedure), qualifie par son schema.</summary>
public sealed record DatabaseObjectEntry(string Schema, string Name)
{
    public string QualifiedName => $"{Schema}.{Name}";
}
