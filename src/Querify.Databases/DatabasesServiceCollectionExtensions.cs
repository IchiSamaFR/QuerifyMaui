using Microsoft.Extensions.DependencyInjection;
using Querify.Databases.Connections;
using Querify.Databases.Enums;
using Querify.Databases.Schemas;
using Querify.Databases.SqlServer;
using Quill.Data.Sql.SqlServer;

namespace Querify.Databases;

/// <summary>Enregistrement des services de la librairie (elle ne depend d'aucun projet de l'application, seulement de Quill.Data.Sql).</summary>
public static class DatabasesServiceCollectionExtensions
{
    /// <summary>Stockage des connexions, resolveurs, cache des catalogues et implementations de chaque moteur.</summary>
    public static IServiceCollection AddDatabases(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // IConnectionSecretStore et IConnectionDescriptorStorage sont fournis par l'hote.
        services.AddSingleton<IConnectionStore, ConnectionStore>();
        services.AddSingleton<IConnectionTesterResolver, ConnectionTesterResolver>();
        services.AddSingleton<IDatabaseSchemaReaderResolver, DatabaseSchemaReaderResolver>();
        services.AddSingleton<IDatabaseSchemaCache, DatabaseSchemaCache>();

        // Un moteur = une implementation de chaque contrat ; les resolveurs les recoivent toutes.
        services.AddSingleton<IConnectionTester, SqlServerConnectionTester>();
        // Lecture du catalogue : celle de Quill.Data.Sql, associee ici au moteur qu'elle sait lire.
        services.AddSingleton(new DatabaseSchemaReaderRegistration(DatabaseProvider.SqlServer, new SqlServerSchemaReader()));

        return services;
    }
}
