using SqlToolManagerMaui.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Services.Editors.Highlighting;

/// <summary>
/// Acces aux definitions de coloration livrees avec l'application.
/// Un nouveau langage s'ajoute en deposant un .xshd dans Resources/Raw/SyntaxHighlights.
/// </summary>
public interface ISyntaxDefinitionStore
{
    /// <summary>Charge la definition du langage (ex. "Sql"), ou null si aucun fichier ne lui correspond.</summary>
    Task<SyntaxDefinition?> GetAsync(string language, CancellationToken cancellationToken = default);
}
