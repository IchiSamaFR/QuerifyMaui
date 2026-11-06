using Querify.Core.Ioc;
using Querify.Databases;
using Querify.Languages;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Sql.Queries;
using Quill.Code.Sql.SqlServer;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Querify;

/// <summary>
/// Enregistrement centralise des dependances de l'application.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services
            .AddDatabases()
            .AddAutoRegisteredServices(
            [
                typeof(ServiceCollectionExtensions).Assembly,
                typeof(LanguageAssemblies).Assembly,
                typeof(SqlQueryExecutor).Assembly,
                typeof(LinqQueryExecutor).Assembly
            ]);
    }

    /// <summary>
    /// Enregistre toutes les classes marquees <see cref="AutoRegisterAttribute"/> des assemblies.
    /// Plusieurs implementations d'un meme service (testeurs, executeurs...) sont toutes
    /// ajoutees : les resolveurs les recoivent en <c>IEnumerable</c>.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : scan reflexif assume.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "App non publiee en trimmed : scan reflexif assume.")]
    private static IServiceCollection AddAutoRegisteredServices(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (Type type in assemblies.SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            AutoRegisterAttribute[] attributes = type.GetCustomAttributes<AutoRegisterAttribute>(inherit: false).ToArray();
            if (attributes.Length == 0)
            {
                continue;
            }

            foreach (AutoRegisterAttribute attribute in attributes)
            {
                Type serviceType = attribute.As ?? type;
                if (!serviceType.IsAssignableFrom(type))
                {
                    throw new InvalidOperationException($"{type.Name} n'est pas assignable au service {serviceType.Name} declare dans [AutoRegister].");
                }

                if (attributes.Length == 1)
                {
                    services.Add(new ServiceDescriptor(serviceType, type, attribute.Lifetime));
                }
                else
                {
                    // Plusieurs services : tous renvoient l'instance de la classe, enregistree une fois.
                    services.Add(new ServiceDescriptor(serviceType, provider => provider.GetRequiredService(type), attribute.Lifetime));
                }
            }

            if (attributes.Length > 1)
            {
                services.Add(new ServiceDescriptor(type, type, attributes[0].Lifetime));
            }
        }

        return services;
    }
}
