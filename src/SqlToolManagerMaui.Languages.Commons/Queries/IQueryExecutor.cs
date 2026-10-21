using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;

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

    /// <summary>
    /// Lit un seul jeu de resultats avec des parametres types (cartes du dashboard) ; <paramref name="database"/>
    /// null = base de la connexion. Contrairement a <see cref="ExecuteAsync"/>, une erreur leve une exception.
    /// </summary>
    Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
