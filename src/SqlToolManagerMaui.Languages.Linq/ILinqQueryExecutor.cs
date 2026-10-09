using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Queries;

namespace SqlToolManagerMaui.Languages.Linq;

/// <summary>Execution d'une requete LINQ sur le DbContext genere a partir du catalogue de la connexion.</summary>
public interface ILinqQueryExecutor
{
    /// <summary>Meme contrat que l'execution SQL : les erreurs sont rapportees dans les messages.</summary>
    Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string query, CancellationToken cancellationToken = default);
}
