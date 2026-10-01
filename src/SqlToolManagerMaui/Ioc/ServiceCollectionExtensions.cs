
using SqlToolManagerMaui.Services;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.Services.Queries;
using SqlToolManagerMaui.ViewModels;
using SqlToolManagerMaui.ViewModels.Pages;
using SqlToolManagerMaui.ViewModels.Pages.Connections;
using SqlToolManagerMaui.Views.Pages;
using SqlToolManagerMaui.Views.Pages.Connections;

namespace SqlToolManagerMaui;

/// <summary>
/// Enregistrement centralisé des dépendances de l'application.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services
            .AddApplicationDomainServices()
            .AddShell()
            .AddPages();
    }

    private static IServiceCollection AddApplicationDomainServices(this IServiceCollection services)
    {

        return services
            .AddConnectionServices()
            .AddQueryProviders()
            .AddQueryExecutors();
    }

    /// <summary>
    /// Enregistre les providers de requêtes et leur résolveur. Un nouveau langage
    /// s'ajoute ici et nulle part ailleurs : le résolveur le découvre par son
    /// enregistrement en tant que <see cref="QueryProviderBase"/>.
    /// </summary>
    private static IServiceCollection AddQueryProviders(this IServiceCollection services)
    {
        // Acces disque de l'editeur (FilePicker / FileSaver).
        services.AddSingleton<IEditorFileService, EditorFileService>();

        // Coloration syntaxique : definitions .xshd + tokenizer.
        services.AddSingleton<ISyntaxConfigurationStore, XshdSyntaxConfigurationStore>();
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighter>();

        return services;
    }

    /// <summary>
    /// Execution des requetes. Un nouveau moteur s'ajoute par un simple
    /// AddSingleton&lt;IQueryExecutor, ...&gt; : le resolveur le decouvre seul.
    /// </summary>
    private static IServiceCollection AddQueryExecutors(this IServiceCollection services)
    {
        services.AddSingleton<IQueryExecutor, SqlServerQueryExecutor>();
        services.AddSingleton<IQueryExecutorResolver, QueryExecutorResolver>();

        return services;
    }

    /// <summary>
    /// Persistance des connexions et testeurs par moteur. Un nouveau moteur s'ajoute
    /// par un simple AddSingleton&lt;IConnectionTester, ...&gt; : le resolveur le decouvre seul.
    /// </summary>
    private static IServiceCollection AddConnectionServices(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionStore, ConnectionStore>();

        services.AddSingleton<IConnectionTester, SqlServerConnectionTester>();
        services.AddSingleton<IConnectionTesterResolver, ConnectionTesterResolver>();

        // Catalogue de l'explorateur d'objets : un lecteur par moteur, comme les testeurs.
        services.AddSingleton<IDatabaseSchemaReader, SqlServerSchemaReader>();
        services.AddSingleton<IDatabaseSchemaReaderResolver, DatabaseSchemaReaderResolver>();

        services.AddSingleton<IConnectionDialogService, ConnectionDialogService>();

        return services;
    }

    private static IServiceCollection AddShell(this IServiceCollection services)
    {
        services.AddSingleton<AppShell>();
        services.AddSingleton<ShellVM>();

        return services;
    }

    private static IServiceCollection AddPages(this IServiceCollection services)
    {
        services.AddSingleton<QueryPage>();
        services.AddSingleton<QueryVM>();

        services.AddTransient<ConnectionExplorerView>();
        services.AddSingleton<ConnectionExplorerVM>();

        // Popup "Se connecter" : VM neuf a chaque ouverture.
        services.AddTransient<ConnectionEditorVM>();

        services.AddTransient<ConnectionExplorerView>();
        services.AddSingleton<ConnectionExplorerVM>();

        return services;
    }
}
