namespace SqlToolManagerMaui.Databases.Models.Schemas;

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