using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Languages.Linq.Compilation;

namespace SqlToolManagerMaui.Languages.Linq.Modeling;

/// <summary>
/// DbContext compile d'une base de la connexion, partage par l'execution et l'autocompletion :
/// la premiere des deux paie la generation, l'autre la retrouve.
/// </summary>
public interface ILinqModelCache
{
    /// <summary>Modele de <paramref name="database"/> ; null = base par defaut de la connexion.</summary>
    Task<LinqModelAssembly> GetModelAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken = default);
}
