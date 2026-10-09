using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Ioc;

namespace SqlToolManagerMaui.Databases.Schemas;

/// <inheritdoc cref="IDatabaseSchemaReaderResolver"/>
[AutoRegister(As = typeof(IDatabaseSchemaReaderResolver))]
public sealed class DatabaseSchemaReaderResolver : IDatabaseSchemaReaderResolver
{
    private readonly Dictionary<DatabaseProvider, IDatabaseSchemaReader> _readers;

    public DatabaseSchemaReaderResolver(IEnumerable<IDatabaseSchemaReader> readers)
    {
        ArgumentNullException.ThrowIfNull(readers);
        _readers = readers.ToDictionary(r => r.Provider);
    }

    public IDatabaseSchemaReader Resolve(DatabaseProvider provider)
        => _readers.TryGetValue(provider, out var reader)
            ? reader
            : throw new NotSupportedException($"Aucun lecteur de catalogue enregistre pour le moteur {provider}.");
}
