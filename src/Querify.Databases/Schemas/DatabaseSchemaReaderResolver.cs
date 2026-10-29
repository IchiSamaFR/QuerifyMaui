using Querify.Databases.Common;
using Querify.Databases.Enums;
using Quill.Data.Sql;

namespace Querify.Databases.Schemas;

/// <inheritdoc cref="IDatabaseSchemaReaderResolver"/>
public sealed class DatabaseSchemaReaderResolver : IDatabaseSchemaReaderResolver
{
    private readonly KeyedServiceMap<DatabaseProvider, DatabaseSchemaReaderRegistration> _readers;

    public DatabaseSchemaReaderResolver(IEnumerable<DatabaseSchemaReaderRegistration> readers)
    {
        _readers = new(readers, r => r.Provider, "lecteur de catalogue");
    }

    public ISqlSchemaReader Resolve(DatabaseProvider provider) => _readers.Resolve(provider).Reader;
}
