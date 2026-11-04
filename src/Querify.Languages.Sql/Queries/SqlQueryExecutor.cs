using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Queries;
using Querify.Databases.Enums;
using Querify.Databases.Models.Connections;
using Querify.Languages.Queries;
using Quill.Code.Execution;
using Quill.Code.MySql;
using Quill.Code.Oracle;
using Quill.Code.PostgreSql;
using Quill.Code.Scripting;
using Quill.Code.Sqlite;
using Quill.Code.SqlServer;

namespace Querify.Languages.Sql.Queries;

/// <summary>Point d'entree du langage SQL : delegue a l'executeur de Quill du moteur de la connexion.</summary>
[AutoRegister(As = typeof(IQueryExecutor))]
public sealed class SqlQueryExecutor : IQueryExecutor
{
    private const string ApplicationName = "Querify";

    /// <summary>Un moteur = une entree.</summary>
    private static readonly Dictionary<DatabaseProvider, ISqlQueryExecutor> Executors = new()
    {
        [DatabaseProvider.SqlServer] = new SqlServerQueryExecutor(ApplicationName),
        [DatabaseProvider.MySql] = new MySqlQueryExecutor(ApplicationName),
        [DatabaseProvider.PostgreSql] = new PostgreSqlQueryExecutor(ApplicationName),
        [DatabaseProvider.Oracle] = new OracleQueryExecutor(),
        [DatabaseProvider.Sqlite] = new SqliteQueryExecutor(),
    };

    public SyntaxLanguage Language => SyntaxLanguage.Sql;

    /// <summary>Bilan construit par <see cref="QueryExecutionCollector"/> a partir de ce que l'executeur de Quill remet au fil de l'eau.</summary>
    public Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, IProgress<LogEntry>? liveMessages = null, CancellationToken cancellationToken = default)
    {
        var executor = Resolve(connection);
        var connectionString = GetConnectionString(connection);

        return QueryExecutionCollector.CollectAsync(options => executor.RunAsync(connectionString, script, options, cancellationToken), liveMessages);
    }

    public Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
        => Resolve(connection).ReadAsync(GetConnectionString(connection), database, query, parameters, maxRows, cancellationToken);

    private static ISqlQueryExecutor Resolve(DatabaseConnectionInfo connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return Executors.TryGetValue(connection.Provider, out var executor)
            ? executor
            : throw new NotSupportedException($"Aucun exécuteur SQL pour le moteur {connection.Provider}.");
    }

    private static string GetConnectionString(DatabaseConnectionInfo connection)
        => connection.ConnectionString
            ?? throw new InvalidOperationException($"La connexion {connection.Name} n'est pas ouverte.");
}
