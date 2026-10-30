using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Enums;
using Querify.Databases.Models.Connections;
using Querify.Languages.Commons.Queries;
using Quill.Code.SqlServer;
using System.Diagnostics;
using Quill.Code.Scripting;
using Quill.Code.Execution;
using Quill.Code.SqlServer.Execution;
using Querify.Core.Models.Queries;

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
    };

    public SyntaxLanguage Language => SyntaxLanguage.Sql;

    /// <summary>
    /// Bilan d'une execution, construit a partir de ce que l'executeur de Quill remet au fil de l'eau : messages
    /// (affiches aussi en direct), erreurs SQL (niveau Error) et jeux de resultats.
    /// </summary>
    public async Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, IProgress<LogEntry>? liveMessages = null, CancellationToken cancellationToken = default)
    {
        var executor = Resolve(connection);
        var connectionString = GetConnectionString(connection);

        List<QueryResultSet> resultSets = [];
        List<LogEntry> messages = [];
        var gate = new Lock();
        var hasErrors = false;
        var wasCancelled = false;
        var stopwatch = Stopwatch.StartNew();

        var options = new DataRunOptions
        {
            Log = (level, text) =>
            {
                var message = new LogEntry(level, text);
                lock (gate)
                {
                    hasErrors |= message.IsError;
                    messages.Add(message);
                }

                liveMessages?.Report(message);
            },
            Data = resultSet =>
            {
                lock (gate)
                    resultSets.Add(resultSet);
            },
        };

        try
        {
            await executor.RunAsync(connectionString, script, options, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }

        stopwatch.Stop();

        lock (gate)
        {
            if (wasCancelled)
                messages.Add(new LogEntry(LogLevel.Error, "Requête annulée par l'utilisateur."));

            messages.Add(new LogEntry(LogLevel.Information, $"Heure de fin : {DateTime.Now:yyyy-MM-dd HH:mm:ss} (durée {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff})"));

            return new QueryExecutionResult([.. resultSets], [.. messages], stopwatch.Elapsed, hasErrors, wasCancelled);
        }
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
