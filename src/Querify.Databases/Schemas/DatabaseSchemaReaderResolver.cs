using Querify.Databases.Common;
using Querify.Databases.Enums;

namespace Querify.Databases.Schemas;

/// <inheritdoc cref="IDatabaseSchemaReaderResolver"/>
public sealed class DatabaseSchemaReaderResolver : IDatabaseSchemaReaderResolver
{
    private readonly KeyedServiceMap<DatabaseProvider, IDatabaseSchemaReader> _readers;

    public DatabaseSchemaReaderResolver(IEnumerable<IDatabaseSchemaReader> readers)
    {
        _readers = new(readers, r => r.Provider, "lecteur de catalogue");
    }

    public IDatabaseSchemaReader Resolve(DatabaseProvider provider) => _readers.Resolve(provider);
}
