using SqlToolManagerMaui.Databases.Enums;

namespace SqlToolManagerMaui.Databases.Models.Schemas;

/// <summary>Table, vue ou procedure, avec ses colonnes (vides pour une procedure).</summary>
public sealed record DatabaseSchemaObject(
    string Schema,
    string Name,
    DatabaseObjectKind Kind,
    IReadOnlyList<DatabaseSchemaColumn> Columns);
