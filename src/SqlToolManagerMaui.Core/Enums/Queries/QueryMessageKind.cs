namespace SqlToolManagerMaui.Core.Enums.Queries;

/// <summary>Origine d'un message d'execution, pour le distinguer dans l'onglet Messages.</summary>
public enum QueryMessageKind
{
    /// <summary>Message du moteur ou de l'application : duree, lignes affectees, SQL genere...</summary>
    Info,
    Error,
    /// <summary>Sortie ecrite par le script lui-meme (Console.WriteLine, PRINT).</summary>
    Output,
}
