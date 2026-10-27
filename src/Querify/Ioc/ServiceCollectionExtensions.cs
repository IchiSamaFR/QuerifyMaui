using Querify.Core.Ioc;
using Querify.Databases;
using Querify.Languages;
using Quill.Code.CSharp;
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
            .AddQuillCSharp()
            .AddAutoRegisteredServices([typeof(ServiceCollectionExtensions).Assembly, .. LanguageAssemblies.All]);
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
            AutoRegisterAttribute? attribute = type.GetCustomAttribute<AutoRegisterAttribute>(inherit: false);
            if (attribute is null)
            {
                continue;
            }

            Type serviceType = attribute.As ?? type;
            if (!serviceType.IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"{type.Name} n'est pas assignable au service {serviceType.Name} declare dans [AutoRegister].");
            }

            services.Add(new ServiceDescriptor(serviceType, type, attribute.Lifetime));
        }

        return services;
    }
}
