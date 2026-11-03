using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Queries;
using Querify.Databases.Models.Connections;
using Querify.Databases.Schemas;
using Querify.Languages.Queries;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using Quill.Data.Sql;

namespace Querify.Languages.Linq.Queries;

/// <summary>
/// Point d'entree du langage LINQ : delegue a l'executeur LINQ de Quill, sur la structure de la base lue par le cache
/// des schemas (meme instance que l'editeur, donc meme DbContext compile).
/// </summary>
[AutoRegister(As = typeof(IQueryExecutor))]
public sealed class LinqQueryExecutor : IQueryExecutor
{
    private readonly IDatabaseSchemaCache _schemas;

    public LinqQueryExecutor(IDatabaseSchemaCache schemas)
    {
        _schemas = schemas;
    }

    public SyntaxLanguage Language => SyntaxLanguage.Linq;

    /// <summary>Bilan construit par <see cref="QueryExecutionCollector"/>, comme pour le SQL.</summary>
    public Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string query, IProgress<LogEntry>? liveMessages = null, CancellationToken cancellationToken = default)
    {
        var connectionString = GetConnectionString(connection);
        var executor = GetExecutor(connection);

        return QueryExecutionCollector.CollectAsync(options => RunAsync(executor, connection, connectionString, query, options, cancellationToken), liveMessages);
    }

    public async Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
    {
        var connectionString = GetConnectionString(connection);
        var executor = GetExecutor(connection);
        var schema = await _schemas.GetSchemaAsync(connection, string.IsNullOrWhiteSpace(database) ? null : database, cancellationToken);

        return await executor.ReadAsync(connectionString, database, schema, query, parameters, maxRows, cancellationToken);
    }

    private async Task RunAsync(Quill.Code.CSharp.Linq.LinqQueryExecutor executor, DatabaseConnectionInfo connection, string connectionString, string query, DataRunOptions options, CancellationToken cancellationToken)
    {
        SqlDatabaseSchema schema;
        try
        {
            schema = await _schemas.GetSchemaAsync(connection, database: null, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Base injoignable, droits... : comme une erreur de connexion SQL, un message plutot qu'une exception.
            options.Log?.Invoke(new LogEntry(LogLevel.Error, ex.Message));
            return;
        }

        await executor.RunAsync(connectionString, schema, query, options, cancellationToken);
    }

    /// <summary>Executeur de Quill sur le moteur de la connexion (meme fournisseur que l'editeur).</summary>
    private static Quill.Code.CSharp.Linq.LinqQueryExecutor GetExecutor(DatabaseConnectionInfo connection)
        => new(LinqDatabaseProviders.Find(connection)
            ?? throw new NotSupportedException($"Aucun moteur LINQ pour {connection.Provider}."));

    private static string GetConnectionString(DatabaseConnectionInfo connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.ConnectionString
            ?? throw new InvalidOperationException($"La connexion {connection.Name} n'est pas ouverte.");
    }
}
