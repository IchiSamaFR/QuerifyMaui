using Querify.Core.Enums.Editors.Highlighting;

namespace Querify.Services.Editors;

/// <summary>Modeles inseres dans les nouveaux scripts : Resources/Raw/Templates/&lt;langage&gt;&lt;extension&gt;.</summary>
public interface IDocumentTemplateStore
{
    /// <summary>Texte du modele du langage, vide s'il n'en a pas.</summary>
    Task<string> GetAsync(SyntaxLanguage language, CancellationToken cancellationToken = default);
}
