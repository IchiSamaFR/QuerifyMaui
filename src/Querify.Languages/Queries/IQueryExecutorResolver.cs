using Querify.Core.Enums.Editors.Highlighting;

namespace Querify.Languages.Queries;

/// <summary>
/// Resout l'executeur d'un langage. Un nouveau langage se declare uniquement
/// par son enregistrement en tant que <see cref="IQueryExecutor"/>.
/// </summary>
public interface IQueryExecutorResolver
{
    IQueryExecutor Resolve(SyntaxLanguage language);
}
