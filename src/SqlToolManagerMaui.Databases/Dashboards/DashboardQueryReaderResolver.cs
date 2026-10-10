using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Ioc;

namespace SqlToolManagerMaui.Databases.Dashboards;

/// <inheritdoc cref="IDashboardQueryReaderResolver"/>
[AutoRegister(As = typeof(IDashboardQueryReaderResolver))]
public sealed class DashboardQueryReaderResolver : IDashboardQueryReaderResolver
{
    private readonly Dictionary<DatabaseProvider, IDashboardQueryReader> _readers;

    public DashboardQueryReaderResolver(IEnumerable<IDashboardQueryReader> readers)
    {
        ArgumentNullException.ThrowIfNull(readers);
        _readers = readers.ToDictionary(r => r.Provider);
    }

    public IDashboardQueryReader Resolve(DatabaseProvider provider)
        => _readers.TryGetValue(provider, out var reader)
            ? reader
            : throw new NotSupportedException($"Aucun lecteur de dashboard enregistré pour le moteur {provider}.");
}
