using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Languages.Commons.Queries;

namespace SqlToolManagerMaui.Languages.Queries;

/// <summary>
/// Resout l'executeur d'un langage. Un nouveau langage se declare uniquement
/// par son enregistrement en tant que <see cref="IQueryExecutor"/>.
/// </summary>
public interface IQueryExecutorResolver
{
    IQueryExecutor Resolve(SyntaxLanguage language);
}
