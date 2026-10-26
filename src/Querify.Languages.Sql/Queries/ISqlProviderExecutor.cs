using Querify.Core.Models.Queries;
using Querify.Databases.Enums;

namespace Querify.Languages.Sql.Queries;

/// <summary>
/// Execution d'un script SQL sur un moteur. Une implementation par moteur, resolue par <see cref="Provider"/>
/// dans <see cref="SqlQueryExecutor"/>.
/// </summary>
public interface ISqlProviderExecutor
{
    DatabaseProvider Provider { get; }

    /// <summary>
    /// Execute le script et collecte resultats et messages. Les erreurs SQL ne levent pas
    /// d'exception : elles sont rapportees dans <see cref="QueryExecutionResult.Messages"/>.
    /// </summary>
    Task<QueryExecutionResult> ExecuteAsync(string connectionString, string script, CancellationToken cancellationToken = default);
}
