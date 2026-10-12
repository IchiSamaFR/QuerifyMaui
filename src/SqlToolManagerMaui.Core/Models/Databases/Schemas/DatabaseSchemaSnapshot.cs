namespace SqlToolManagerMaui.Core.Models.Databases.Schemas;

/// <summary>Catalogue complet d'une base, charge en une passe pour l'autocompletion.</summary>
public sealed class DatabaseSchemaSnapshot
{
    public required string Database { get; init; }

    public required IReadOnlyList<DatabaseSchemaObject> Objects { get; init; }

    /// <summary>Schemas possedant au moins un objet utilisateur.</summary>
    public IReadOnlyList<string> Schemas => field ??= Objects.Select(o => o.Schema).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary>Recherche de schema en O(1) : HasSchema est appele a chaque "nom." tape.</summary>
    private HashSet<string> SchemaSet => field ??= new HashSet<string>(Schemas, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Objets par nom, dbo en tete : Find est rappele pour chaque table de la requete,
    /// un parcours lineaire du catalogue (milliers d'objets) a chaque fois coutait cher.
    /// </summary>
    private Dictionary<string, DatabaseSchemaObject[]> ByName => field ??= Objects
        .GroupBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            g => g.Key,
            g => g.OrderBy(o => string.Equals(o.Schema, "dbo", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ToArray(),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>Objet par nom ; sans schema, dbo est prefere comme le ferait SQL Server.</summary>
    public DatabaseSchemaObject? Find(string? schema, string name)
    {
        if (!ByName.TryGetValue(name, out var candidates))
            return null;

        if (schema is null)
            return candidates[0];

        foreach (var candidate in candidates)
        {
            if (string.Equals(candidate.Schema, schema, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }

    public bool HasSchema(string schema) => SchemaSet.Contains(schema);
}
