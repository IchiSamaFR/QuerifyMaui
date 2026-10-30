namespace Querify.Core.Enums.Queries;

/// <summary>Origine d'un message d'execution, pour le distinguer dans l'onglet Messages.</summary>
public enum QueryMessageKind
{
    /// <summary>Message de l'application : duree, contexte, heure de fin...</summary>
    Info,
    Error,
    /// <summary>Sortie de l'execution : Log.* du script, PRINT, lignes affectees, SQL genere.</summary>
    Output,
}
