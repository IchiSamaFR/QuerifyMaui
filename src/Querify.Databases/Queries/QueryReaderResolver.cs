using Querify.Databases.Common;
using Querify.Databases.Enums;

namespace Querify.Databases.Queries;

/// <inheritdoc cref="IQueryReaderResolver"/>
public sealed class QueryReaderResolver : IQueryReaderResolver
{
    private readonly KeyedServiceMap<DatabaseProvider, IQueryReader> _readers;

    public QueryReaderResolver(IEnumerable<IQueryReader> readers)
    {
        _readers = new(readers, r => r.Provider, "lecteur de requêtes");
    }

    public IQueryReader Resolve(DatabaseProvider provider) => _readers.Resolve(provider);
}
