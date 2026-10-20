using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Databases.Common;
using SqlToolManagerMaui.Databases.Enums;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Languages.Commons.Queries;

namespace SqlToolManagerMaui.Languages.Sql.Queries;

/// <summary>Point d'entree du langage SQL : delegue au moteur de la connexion.</summary>
[AutoRegister(As = typeof(IQueryExecutor))]
public sealed class SqlQueryExecutor : IQueryExecutor
{
    private readonly KeyedServiceMap<DatabaseProvider, ISqlProviderExecutor> _executors;

    public SqlQueryExecutor(IEnumerable<ISqlProviderExecutor> executors)
    {
        _executors = new(executors, e => e.Provider, "exécuteur SQL");
    }

    public SyntaxLanguage Language => SyntaxLanguage.Sql;

    public Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var executor = _executors.Resolve(connection.Provider);
        var connectionString = connection.ConnectionString
            ?? throw new InvalidOperationException($"La connexion {connection.Name} n'est pas ouverte.");

        return executor.ExecuteAsync(connectionString, script, cancellationToken);
    }
}
