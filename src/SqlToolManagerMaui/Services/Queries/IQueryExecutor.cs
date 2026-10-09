using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Queries;

namespace SqlToolManagerMaui.Services.Queries;

/// <summary>
/// Execution d'un script sur une base. Une implementation par moteur, resolue par <see cref="Provider"/>.
/// </summary>
public interface IQueryExecutor
{
    DatabaseProvider Provider { get; }

    /// <summary>
    /// Execute le script et collecte resultats et messages. Les erreurs SQL ne levent pas
    /// d'exception : elles sont rapportees dans <see cref="QueryExecutionResult.Messages"/>.
    /// </summary>
    Task<QueryExecutionResult> ExecuteAsync(string connectionString, string script, CancellationToken cancellationToken = default);
}
