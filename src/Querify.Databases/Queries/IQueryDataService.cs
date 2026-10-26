using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;

namespace Querify.Databases.Queries;

/// <summary>Lecture d'une requete parametree sur une connexion ouverte, quel que soit son moteur.</summary>
public interface IQueryDataService
{
    Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
