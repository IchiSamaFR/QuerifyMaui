namespace SqlToolManagerMaui.Databases.Enums;

/// <summary>Type d'un parametre de requete, traduit par chaque moteur dans son propre type.</summary>
public enum QueryParameterType
{
    Text,
    Integer,
    Decimal,
    Date,
    DateTime,
    Boolean,
    Guid,
}
