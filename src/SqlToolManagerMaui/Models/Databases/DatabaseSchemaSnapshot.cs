namespace SqlToolManagerMaui.Models.Databases;

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

/// <summary>Table, vue ou procedure, avec ses colonnes (vides pour une procedure).</summary>
public sealed record DatabaseSchemaObject(string Schema, string Name, DatabaseObjectKind Kind, IReadOnlyList<DatabaseSchemaColumn> Columns);

/// <param name="DataType">Type deja formate pour l'affichage (ex. "nvarchar(50)").</param>
/// <param name="SqlType">Type systeme de base, alias resolus (ex. "nvarchar") : sert au mapping .NET.</param>
/// <param name="PrimaryKeyOrdinal">Position dans la cle primaire (base 1), 0 hors cle.</param>
public sealed record DatabaseSchemaColumn(
    string Name,
    string DataType,
    string SqlType = "",
    bool IsNullable = true,
    int PrimaryKeyOrdinal = 0,
    byte Precision = 0,
    byte Scale = 0)
{
    public bool IsPrimaryKey => PrimaryKeyOrdinal > 0;
}
