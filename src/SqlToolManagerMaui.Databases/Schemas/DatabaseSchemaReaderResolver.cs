using SqlToolManagerMaui.Databases.Common;
using SqlToolManagerMaui.Databases.Enums;

namespace SqlToolManagerMaui.Databases.Schemas;

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
