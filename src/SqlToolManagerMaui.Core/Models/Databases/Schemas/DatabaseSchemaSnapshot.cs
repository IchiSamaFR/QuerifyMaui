namespace SqlToolManagerMaui.Core.Models.Databases.Schemas;

/// <summary>Catalogue complet d'une base, charge en une passe pour l'autocompletion.</summary>
public sealed class DatabaseSchemaSnapshot
{
    public required string Database { get; init; }

    public required IReadOnlyList<DatabaseSchemaObject> Objects { get; init; }

    /// <summary>Schemas possedant au moins un objet utilisateur.</summary>
    public IReadOnlyList<string> Schemas => field ??= Objects.Select(o => o.Schema).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary>Objet par nom ; sans schema, dbo est prefere comme le ferait SQL Server.</summary>
    public DatabaseSchemaObject? Find(string? schema, string name)
    {
        var candidates = Objects.Where(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));

        return schema is null
            ? candidates.OrderBy(o => string.Equals(o.Schema, "dbo", StringComparison.OrdinalIgnoreCase) ? 0 : 1).FirstOrDefault()
            : candidates.FirstOrDefault(o => string.Equals(o.Schema, schema, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasSchema(string schema) => Schemas.Contains(schema, StringComparer.OrdinalIgnoreCase);
}
