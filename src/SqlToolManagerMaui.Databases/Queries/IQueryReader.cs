using SqlToolManagerMaui.Databases.Enums;
using SqlToolManagerMaui.Databases.Models.Queries;

namespace SqlToolManagerMaui.Databases.Queries;

/// <summary>Requete parametree d'un moteur. Une implementation par moteur.</summary>
public interface IQueryReader
{
    DatabaseProvider Provider { get; }

    /// <summary>Premier jeu de resultats (au plus <paramref name="maxRows"/> lignes) ; null = base par defaut de la connexion.</summary>
    Task<QueryResultSet> ReadAsync(string connectionString, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default);
}
