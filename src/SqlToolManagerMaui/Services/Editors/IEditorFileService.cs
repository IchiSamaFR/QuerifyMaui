namespace SqlToolManagerMaui.Services.Editors;

/// <summary>Contenu d'un fichier ouvert depuis le disque.</summary>
/// <param name="FilePath">Chemin complet retenu par le selecteur.</param>
/// <param name="Text">Texte decode en UTF-8.</param>
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

    /// <summary>Ecrit le texte a un chemin connu (UTF-8 sans BOM).</summary>
    Task SaveAsync(string filePath, string text, CancellationToken cancellationToken = default);

    /// <summary>Ouvre un selecteur d'enregistrement. Retourne le chemin retenu, ou null si annule.</summary>
    Task<string?> SaveAsAsync(string suggestedFileName, string text, CancellationToken cancellationToken = default);
}
