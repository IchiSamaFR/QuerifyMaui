using Querify.Databases.Models.Connections;

namespace Querify.Databases.Connections;

/// <summary>
/// Stockage mixte : les descripteurs ne contiennent que des metadonnees affichables, la chaine de
/// connexion complete vit dans le coffre. Toutes les ecritures passent par un semaphore : la liste est reecrite en entier.
/// </summary>
public sealed class ConnectionStore : IConnectionStore
{
    private readonly IConnectionSecretStore _secrets;
    private readonly IConnectionDescriptorStorage _storage;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private List<DatabaseConnectionInfo>? _cache;
    private Action<IReadOnlyList<DatabaseConnectionInfo>>? _onChanged;

    public ConnectionStore(IConnectionSecretStore secrets, IConnectionDescriptorStorage storage)
    {
        _secrets = secrets;
        _storage = storage;
    }

    /// <inheritdoc/>
    public async Task<string?> GetConnectionStringAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await _secrets.GetAsync(id);
        }
        catch (Exception)
        {
            // Coffre indisponible ou entree illisible (changement de machine, profil recree) :
            // on traite comme "chaine absente" plutot que de faire echouer la page.
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await LoadUnsafeAsync(forWrite: false, cancellationToken);
            _onChanged?.Invoke(Sorted(items));
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> Subscribe(Action<IReadOnlyList<DatabaseConnectionInfo>> action, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _onChanged += action;
            var items = await LoadUnsafeAsync(forWrite: false, cancellationToken);
            _onChanged?.Invoke(Sorted(items));
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public void Unsubscribe(Action<IReadOnlyList<DatabaseConnectionInfo>> action) => _onChanged -= action;

    /// <inheritdoc/>
    public async Task<bool> OpenConnectionAsync(DatabaseConnectionInfo info, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(info);

        if (info.IsConnected)
            return true;

        var connectionString = await GetConnectionStringAsync(info.Id, cancellationToken);
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        info.ConnectionString = connectionString;
        return true;
    }

    /// <inheritdoc/>
    public bool CloseConnection(DatabaseConnectionInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        return info.CloseConnection();
    }

    /// <inheritdoc/>
    public async Task SaveAsync(DatabaseConnectionInfo info, string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Le secret d'abord : si le coffre refuse, aucun descripteur orphelin n'est ecrit.
        await _secrets.SetAsync(info.Id, connectionString);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await LoadUnsafeAsync(forWrite: true, cancellationToken);
            var index = items.FindIndex(i => i.Id == info.Id);

            info.UpdatedAt = DateTimeOffset.Now;
            if (index >= 0)
            {
                // Mise a jour sur place : l'instance en cache est partagee avec l'explorateur
                // et le Picker, ils voient les changements sans rechargement.
                var existing = items[index];
                info.CreatedAt = existing.CreatedAt;
                existing.Name = info.Name;
                existing.Provider = info.Provider;
                existing.Server = info.Server;
                existing.Database = info.Database;
                existing.UpdatedAt = info.UpdatedAt;

                // Connexion deja ouverte : la nouvelle chaine remplace l'ancienne.
                if (existing.IsConnected)
                    existing.ConnectionString = connectionString;
            }
            else
            {
                // Le descripteur du formulaire est un brouillon : le cache garde sa propre instance.
                items.Add(info.Clone());
            }

            await SaveUnsafeAsync(items, cancellationToken);
            _onChanged?.Invoke(Sorted(items));
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await LoadUnsafeAsync(forWrite: true, cancellationToken);
            var removed = items.FirstOrDefault(i => i.Id == id);
            if (removed is not null)
            {
                items.Remove(removed);
                removed.CloseConnection();
                await SaveUnsafeAsync(items, cancellationToken);
            }
            _onChanged?.Invoke(Sorted(items));
        }
        finally
        {
            _gate.Release();
        }

        try
        {
            _secrets.Remove(id);
        }
        catch (Exception)
        {
            // Le descripteur est parti : une entree residuelle du coffre est sans effet.
        }
    }

    private static List<DatabaseConnectionInfo> Sorted(List<DatabaseConnectionInfo> items) => items.OrderBy(c => c.Name).ToList();

    /// <summary>
    /// Source illisible : rien n'est mis en cache (relue au prochain appel) et une ecriture
    /// (<paramref name="forWrite"/>) est refusee, sinon elle effacerait les connexions enregistrees.
    /// </summary>
    private async Task<List<DatabaseConnectionInfo>> LoadUnsafeAsync(bool forWrite, CancellationToken cancellationToken)
    {
        if (_cache is not null)
            return _cache;

        if (await _storage.ReadAsync(cancellationToken) is { } items)
            return _cache = items;

        // En ecriture, l'appelant recoit l'erreur (RunAsBusyAsync ou popup la journalisent).
        if (forWrite)
            throw new IOException("Connexions illisibles : modification annulée pour ne pas écraser les connexions enregistrées.");

        return [];
    }

    private async Task SaveUnsafeAsync(List<DatabaseConnectionInfo> items, CancellationToken cancellationToken)
    {
        await _storage.WriteAsync(items, cancellationToken);
        _cache = items;
    }
}
