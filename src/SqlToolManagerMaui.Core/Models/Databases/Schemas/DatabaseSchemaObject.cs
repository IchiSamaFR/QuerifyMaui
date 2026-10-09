using SqlToolManagerMaui.Core.Models.Databases.Connections;

namespace SqlToolManagerMaui.Core.Models.Databases.Schemas;

/// <summary>Table, vue ou procedure, avec ses colonnes (vides pour une procedure).</summary>
public sealed record DatabaseSchemaObject(
    string Schema,
    string Name,
    DatabaseObjectKind Kind,
    IReadOnlyList<DatabaseSchemaColumn> Columns);
