using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Queries;

namespace SqlToolManagerMaui.Databases.Dashboards;

/// <summary>Requete d'une carte, avec les parametres du dashboard. Une implementation par moteur.</summary>
public interface IDashboardQueryReader
{
    DatabaseProvider Provider { get; }

    /// <summary>Premier jeu de resultats (au plus <paramref name="maxRows"/> lignes) ; null = base par defaut de la connexion.</summary>
    Task<QueryResultSet> ReadAsync(string connectionString, string? database, string query, IReadOnlyList<DashboardQueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
