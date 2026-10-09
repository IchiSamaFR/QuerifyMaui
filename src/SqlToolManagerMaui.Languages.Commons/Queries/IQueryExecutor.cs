using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Queries;

namespace SqlToolManagerMaui.Languages.Commons.Queries;

/// <summary>
/// Execution d'un script sur une connexion. Une implementation par langage, resolue par <see cref="Language"/>.
/// </summary>
public interface IQueryExecutor
{
    SyntaxLanguage Language { get; }

    /// <summary>
    /// Execute le script sur la connexion ouverte et collecte resultats et messages. Les erreurs du
    /// langage ne levent pas d'exception : elles sont rapportees dans <see cref="QueryExecutionResult.Messages"/>.
    /// </summary>
    Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, CancellationToken cancellationToken = default);
}
