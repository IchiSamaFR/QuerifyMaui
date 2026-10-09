using SqlToolManagerMaui.Core.Models.Databases.Connections;

namespace SqlToolManagerMaui.Services.Linq;

/// <summary>
/// DbContext compile de la base par defaut d'une connexion, partage par l'execution et
/// l'autocompletion : la premiere des deux paie la generation, l'autre la retrouve.
/// </summary>
public interface ILinqModelCache
{
    Task<LinqModelAssembly> GetModelAsync(DatabaseConnectionInfo connection, CancellationToken cancellationToken = default);
}
