using SqlToolManagerMaui.Databases.Enums;

namespace SqlToolManagerMaui.Databases.Queries;

/// <inheritdoc cref="IQueryReaderResolver"/>
public sealed class QueryReaderResolver : IQueryReaderResolver
{
    private readonly Dictionary<DatabaseProvider, IQueryReader> _readers;

    public QueryReaderResolver(IEnumerable<IQueryReader> readers)
    {
        ArgumentNullException.ThrowIfNull(readers);
        _readers = readers.ToDictionary(r => r.Provider);
    }

    public IQueryReader Resolve(DatabaseProvider provider)
        => _readers.TryGetValue(provider, out var reader)
            ? reader
            : throw new NotSupportedException($"Aucun lecteur de requêtes enregistré pour le moteur {provider}.");
}
