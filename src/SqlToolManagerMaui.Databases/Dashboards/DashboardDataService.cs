using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Queries;

namespace SqlToolManagerMaui.Databases.Dashboards;

/// <inheritdoc cref="IDashboardDataService"/>
[AutoRegister(As = typeof(IDashboardDataService))]
public sealed class DashboardDataService : IDashboardDataService
{
    private readonly IDashboardQueryReaderResolver _readers;

    public DashboardDataService(IDashboardQueryReaderResolver readers)
    {
        _readers = readers;
    }

    public Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<DashboardQueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var connectionString = connection.ConnectionString
            ?? throw new InvalidOperationException($"La connexion {connection.Name} n'est pas ouverte.");

        return _readers.Resolve(connection.Provider).ReadAsync(connectionString, database, query, parameters, maxRows, cancellationToken);
    }
}
