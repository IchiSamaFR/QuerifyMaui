using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;

namespace SqlToolManagerMaui.Databases.Queries;

/// <summary>Lecture d'une requete parametree sur une connexion ouverte, quel que soit son moteur.</summary>
public interface IQueryDataService
{
    Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
