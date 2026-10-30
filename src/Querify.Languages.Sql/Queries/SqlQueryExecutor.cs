using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Enums;
using Querify.Databases.Models.Connections;
using Querify.Languages.Commons.Queries;
using Quill.Code.SqlServer;
using Quill.Code.Execution;
using Quill.Code.SqlServer.Execution;

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

    public Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, IProgress<QueryMessage>? liveMessages = null, CancellationToken cancellationToken = default)
    {
        // PRINT et messages du serveur affiches des leur emission ; ils figurent aussi dans le resultat final.
        var options = liveMessages is null
            ? null
            : new DataRunOptions { Log = (_, text) => liveMessages.Report(new QueryMessage(text, QueryMessageKind.Output)) };

        return Resolve(connection).ExecuteAsync(GetConnectionString(connection), script, options, cancellationToken);
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
