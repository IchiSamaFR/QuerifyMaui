namespace SqlToolManagerMaui.Models.Databases;

/// <summary>Colonne d'une table ou d'une vue.</summary>
/// <param name="Name">Nom de la colonne.</param>
/// <param name="DataType">Type deja formate par le moteur (ex. "nvarchar(50)", "decimal(18,2)").</param>
public sealed record DatabaseColumnEntry(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsForeignKey,
    bool IsIdentity,
    bool IsComputed)
{
    /// <summary>Description facon SSMS : "PK, FK, int, not null".</summary>
    public string Description
    {
        get
        {
            var parts = new List<string>(5);
            if (IsPrimaryKey)
                parts.Add("PK");
            if (IsForeignKey)
                parts.Add("FK");
            parts.Add(IsComputed ? $"calculee, {DataType}" : DataType);
            if (IsIdentity)
                parts.Add("identity");
            parts.Add(IsNullable ? "null" : "not null");
            return string.Join(", ", parts);
        }
    }
}
