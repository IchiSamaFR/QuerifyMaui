using Querify.Databases.Enums;

namespace Querify.Databases.Models.Queries;

/// <summary>Parametre envoye avec une requete (null = NULL SQL), jamais concatene au texte.</summary>
public sealed record QueryParameter(string Name, QueryParameterType Type, object? Value);
