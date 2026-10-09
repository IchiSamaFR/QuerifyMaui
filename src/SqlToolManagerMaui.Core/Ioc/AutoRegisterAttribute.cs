using Microsoft.Extensions.DependencyInjection;

namespace SqlToolManagerMaui.Core.Ioc;

/// <summary>
/// Marque une classe pour un enregistrement automatique dans le conteneur
/// (scan realise par <c>ServiceCollectionExtensions</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AutoRegisterAttribute : Attribute
{
    /// <summary>Duree de vie ; <see cref="ServiceLifetime.Singleton"/> par defaut.</summary>
    public ServiceLifetime Lifetime { get; init; } = ServiceLifetime.Singleton;

    /// <summary>Type expose (interface ou classe de base) ; la classe elle-meme par defaut.</summary>
    public Type? As { get; init; }
}
