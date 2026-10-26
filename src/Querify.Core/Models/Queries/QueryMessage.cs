using Querify.Core.Enums.Queries;

namespace Querify.Core.Models.Queries;

/// <summary>Message produit par l'execution : PRINT, sortie console, lignes affectees ou erreur.</summary>
public sealed record QueryMessage(string Text, QueryMessageKind Kind = QueryMessageKind.Info)
{
    public bool IsError => Kind == QueryMessageKind.Error;
}
