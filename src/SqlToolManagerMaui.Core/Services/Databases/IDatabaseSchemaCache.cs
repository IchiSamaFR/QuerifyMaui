using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Databases.Schemas;

namespace SqlToolManagerMaui.Core.Services.Databases;

/// <summary>
/// Catalogues mis en memoire pour l'autocompletion : charges au premier besoin, partages
/// entre onglets, et oublies quand la connexion change de chaine ou sur <see cref="Invalidate"/>.
/// </summary>
public interface IDatabaseSchemaCache
{
    /// <summary>Catalogue d'une base ; null = base de la connexion.</summary>
    Task<DatabaseSchemaSnapshot> GetSchemaAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatabaseCatalogEntry>> GetDatabasesAsync(DatabaseConnectionInfo connection, CancellationToken cancellationToken = default);

    /// <summary>Oublie tout ce qui a ete charge pour cette connexion.</summary>
    void Invalidate(DatabaseConnectionInfo connection);
}
