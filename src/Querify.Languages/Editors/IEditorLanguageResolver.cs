using Querify.Core.Enums.Editors.Highlighting;
using Querify.Languages.Commons.Editors;

namespace Querify.Languages.Editors;

/// <summary>
/// Resout les services d'editeur d'un langage. Un nouveau langage se declare uniquement
/// par son enregistrement en tant que <see cref="IEditorLanguage"/>.
/// </summary>
public interface IEditorLanguageResolver
{
    /// <summary>Null pour un langage sans autocompletion ni analyse.</summary>
    IEditorLanguage? Find(SyntaxLanguage language);
}
