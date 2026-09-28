using SqlToolManagerMaui.Models.Databases;

namespace SqlToolManagerMaui.Services.Queries;

/// <inheritdoc cref="IQueryExecutorResolver"/>
public sealed class QueryExecutorResolver : IQueryExecutorResolver
{
    private readonly Dictionary<DatabaseProvider, IQueryExecutor> _executors;

    public QueryExecutorResolver(IEnumerable<IQueryExecutor> executors)
    {
        ArgumentNullException.ThrowIfNull(executors);
        _executors = executors.ToDictionary(e => e.Provider);
    }

    public IQueryExecutor Resolve(DatabaseProvider provider)
        => _executors.TryGetValue(provider, out var executor)
            ? executor
            : throw new NotSupportedException($"Aucun exécuteur enregistré pour le moteur {provider}.");
}
