using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Queries;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;

namespace Querify.Languages.Commons.Queries;

/// <summary>
/// Execution d'un script sur une connexion. Une implementation par langage, resolue par <see cref="Language"/>.
/// </summary>
public interface IQueryExecutor
{
    SyntaxLanguage Language { get; }

    /// <summary>
    /// Execute le script sur la connexion ouverte et collecte resultats et messages. Les erreurs du
    /// langage ne levent pas d'exception : elles sont rapportees dans <see cref="QueryExecutionResult.Messages"/>.
    /// <paramref name="liveMessages"/> recoit en plus certains messages (ex. Log du script) des leur emission, depuis
    /// n'importe quel thread ; ils figurent aussi dans le resultat final.
    /// </summary>
    Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, IProgress<QueryMessage>? liveMessages = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lit un seul jeu de resultats avec des parametres types (cartes du dashboard) ; <paramref name="database"/>
    /// null = base de la connexion. Contrairement a <see cref="ExecuteAsync"/>, une erreur leve une exception.
    /// </summary>
    Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
