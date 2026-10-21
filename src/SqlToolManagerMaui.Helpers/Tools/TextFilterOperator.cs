namespace SqlToolManagerMaui.Helpers.Tools;

/// <summary>Operateur d'un <see cref="TextFilter"/>, deduit du prefixe saisi.</summary>
public enum TextFilterOperator
{
    None,
    Contains,
    NotContains,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    IsNull,
    IsNotNull,
}
