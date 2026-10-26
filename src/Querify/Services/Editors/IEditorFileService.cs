namespace Querify.Services.Editors;

/// <summary>Contenu d'un fichier ouvert depuis le disque.</summary>
/// <param name="FilePath">Chemin complet retenu par le selecteur.</param>
/// <param name="Text">Texte decode selon l'encodage detecte (BOM, UTF-8, sinon ANSI).</param>
public sealed record EditorFileContent(string FilePath, string Text);

/// <summary>
/// Acces disque de l'editeur. Isole le VM des API de picker, qui different par plateforme
/// et ne sont pas testables.
/// </summary>
public interface IEditorFileService
{
    /// <summary>Ouvre un selecteur de fichier. Retourne null si l'utilisateur annule.</summary>
    Task<EditorFileContent?> OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Lit un fichier connu (fichier recent). Retourne null s'il n'existe plus.</summary>
    Task<EditorFileContent?> ReadAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Ecrit le texte a un chemin connu, de facon atomique, dans l'encodage actuel du fichier (UTF-8 avec BOM s'il n'existe pas).</summary>
    Task SaveAsync(string filePath, string text, CancellationToken cancellationToken = default);

    /// <summary>Ouvre un selecteur d'enregistrement. Retourne le chemin retenu, ou null si annule.</summary>
    Task<string?> SaveAsAsync(string suggestedFileName, string text, CancellationToken cancellationToken = default);

    /// <summary>Renomme le fichier dans son dossier ; retourne le nouveau chemin. Leve une IOException si la cible existe.</summary>
    Task<string> RenameAsync(string filePath, string newFileName, CancellationToken cancellationToken = default);

    /// <summary>Ouvre l'explorateur sur le dossier du fichier, le fichier selectionne ; faux s'il n'existe plus.</summary>
    bool RevealInExplorer(string filePath);
}
