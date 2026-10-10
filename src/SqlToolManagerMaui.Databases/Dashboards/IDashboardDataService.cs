using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Queries;

namespace SqlToolManagerMaui.Databases.Dashboards;

/// <summary>Lecture d'une carte sur une connexion ouverte, quel que soit son moteur.</summary>
public interface IDashboardDataService
{
    Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<DashboardQueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
