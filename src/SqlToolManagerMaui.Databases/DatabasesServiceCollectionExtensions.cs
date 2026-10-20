using Microsoft.Extensions.DependencyInjection;
using SqlToolManagerMaui.Databases.Connections;
using SqlToolManagerMaui.Databases.Queries;
using SqlToolManagerMaui.Databases.Schemas;
using SqlToolManagerMaui.Databases.SqlServer;

namespace SqlToolManagerMaui.Databases;

/// <summary>Enregistrement des services de la librairie (elle ne depend d'aucun projet de l'application).</summary>
public static class DatabasesServiceCollectionExtensions
{
    /// <summary>Resolveurs, cache des catalogues et implementations de chaque moteur.</summary>
    public static IServiceCollection AddDatabases(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IConnectionTesterResolver, ConnectionTesterResolver>();
        services.AddSingleton<IDatabaseSchemaReaderResolver, DatabaseSchemaReaderResolver>();
        services.AddSingleton<IDatabaseSchemaCache, DatabaseSchemaCache>();
        services.AddSingleton<IQueryReaderResolver, QueryReaderResolver>();
        services.AddSingleton<IQueryDataService, QueryDataService>();

        // Un moteur = une implementation de chaque contrat ; les resolveurs les recoivent toutes.
        services.AddSingleton<IConnectionTester, SqlServerConnectionTester>();
        services.AddSingleton<IDatabaseSchemaReader, SqlServerSchemaReader>();
        services.AddSingleton<IQueryReader, SqlServerQueryReader>();

        return services;
    }
}
