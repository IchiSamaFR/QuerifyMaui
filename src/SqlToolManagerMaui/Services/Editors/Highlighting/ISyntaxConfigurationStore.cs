using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Services.Editors.Highlighting;

/// <summary>
/// Acces aux configurations de coloration livrees avec l'application.
/// Un nouveau langage s'ajoute en deposant un .xshd dans Resources/Raw/SyntaxHighlights
/// et en ajoutant la valeur correspondante a <see cref="SyntaxLanguage"/>.
/// </summary>
public interface ISyntaxConfigurationStore
{
    /// <summary>Charge la configuration du langage, ou null si aucun fichier ne lui correspond.</summary>
    Task<SyntaxConfiguration?> GetAsync(SyntaxLanguage language, CancellationToken cancellationToken = default);

    /// <summary>Langage dont le .xshd declare l'extension du fichier, ou null si aucun.</summary>
    Task<SyntaxLanguage?> FindLanguageAsync(string filePath, CancellationToken cancellationToken = default);
}
