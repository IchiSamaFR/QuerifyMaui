
using SqlToolManagerMaui.ViewModels.Pages;
using SqlToolManagerMaui.Views.Pages;

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

        return services.AddQueryProviders();
    }

    /// <summary>
    /// Enregistre les providers de requêtes et leur résolveur. Un nouveau langage
    /// s'ajoute ici et nulle part ailleurs : le résolveur le découvre par son
    /// enregistrement en tant que <see cref="QueryProviderBase"/>.
    /// </summary>
    private static IServiceCollection AddQueryProviders(this IServiceCollection services)
    {

        return services;
    }

    private static IServiceCollection AddShell(this IServiceCollection services)
    {
        services.AddSingleton<AppShell>();

        return services;
    }

    private static IServiceCollection AddPages(this IServiceCollection services)
    {
        services.AddSingleton<MainPage>();
        services.AddSingleton<MainVM>();

        return services;
    }
}
