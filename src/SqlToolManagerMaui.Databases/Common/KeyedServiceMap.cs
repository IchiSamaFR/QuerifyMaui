namespace SqlToolManagerMaui.Databases.Common;

/// <summary>Une implementation par cle (moteur, langage) : base commune des resolveurs.</summary>
public sealed class KeyedServiceMap<TKey, TService>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TService> _services = [];
    private readonly string _serviceLabel;

    /// <param name="serviceLabel">Nom du service dans les messages d'erreur (« testeur de connexion »...).</param>
    public KeyedServiceMap(IEnumerable<TService> services, Func<TService, TKey> keySelector, string serviceLabel)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(keySelector);

        _serviceLabel = serviceLabel;
        foreach (var service in services)
        {
            var key = keySelector(service);

            // Deux enregistrements pour la meme cle : erreur de configuration, signalee des le demarrage.
            if (!_services.TryAdd(key, service))
                throw new InvalidOperationException($"Cle {key} enregistree deux fois ({serviceLabel}) : {_services[key]?.GetType().Name} et {service?.GetType().Name}.");
        }
    }

    public TService Resolve(TKey key)
        => _services.TryGetValue(key, out var service)
            ? service
            : throw new NotSupportedException($"Aucun {_serviceLabel} enregistré pour {key}.");
}
