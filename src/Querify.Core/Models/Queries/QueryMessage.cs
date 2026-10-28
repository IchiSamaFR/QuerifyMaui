using Querify.Core.Enums.Queries;

namespace Querify.Core.Models.Queries;

/// <summary>Message produit par l'execution : PRINT, Log du script, lignes affectees ou erreur.</summary>
/// <param name="Level">Niveau d'une sortie Log.* du script ; null pour les autres messages.</param>
public sealed record QueryMessage(string Text, QueryMessageKind Kind = QueryMessageKind.Info, QueryLogLevel? Level = null)
{
    public bool IsError => Kind == QueryMessageKind.Error;
}
