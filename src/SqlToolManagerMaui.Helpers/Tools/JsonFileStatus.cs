namespace SqlToolManagerMaui.Helpers.Tools;

/// <summary>Issue de la lecture d'un fichier JSON par <see cref="JsonFile.ReadAsync{T}"/>.</summary>
public enum JsonFileStatus
{
    Missing,
    Loaded,

    /// <summary>JSON invalide : une copie a ete tentee (<see cref="JsonFileResult{T}.BackupPath"/>).</summary>
    Corrupted,

    /// <summary>Fichier present mais illisible (verrouille, droits) : rien n'a ete copie.</summary>
    Unreadable,
}
