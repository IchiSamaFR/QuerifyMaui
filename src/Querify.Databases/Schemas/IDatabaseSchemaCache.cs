using Querify.Databases.Models.Connections;
using Quill.Data.Sql;

namespace Querify.Databases.Schemas;

/// <summary>
/// Catalogues mis en memoire pour l'autocompletion : charges au premier besoin, partages
/// entre onglets, et oublies quand la connexion change de chaine ou sur <see cref="Invalidate"/>.
/// </summary>
public interface IDatabaseSchemaCache
{
    /// <summary>Catalogue d'une base ; null = base de la connexion.</summary>
    Task<SqlDatabaseSchema> GetSchemaAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(DatabaseConnectionInfo connection, CancellationToken cancellationToken = default);

    /// <summary>Oublie tout ce qui a ete charge pour cette connexion.</summary>
    void Invalidate(DatabaseConnectionInfo connection);
}
