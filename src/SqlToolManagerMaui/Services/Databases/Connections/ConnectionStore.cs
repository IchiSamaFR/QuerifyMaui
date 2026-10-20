using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Helpers.Extensions;
using SqlToolManagerMaui.Helpers.Tools;
using SqlToolManagerMaui.UI.Extensions;

namespace SqlToolManagerMaui.Services.Databases.Connections;

/// <summary>
/// Stockage mixte : le fichier connections.json (AppDataDirectory) ne contient que
/// des metadonnees affichables, la chaine de connexion complete est chiffree par
/// le SecureStorage de la plateforme sous la cle <c>connectionstring:{id}</c>.
/// Toutes les ecritures passent par un semaphore : le fichier est reecrit en entier.
/// </summary>
[AutoRegister(As = typeof(IConnectionStore))]
public sealed class ConnectionStore : IConnectionStore
{
    private const string FileName = "connections.json";
    private const string SecureKeyPrefix = "connectionstring:";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);

    private List<DatabaseConnectionInfo>? _cache;
    private Action<IReadOnlyList<DatabaseConnectionInfo>>? _onChanged = null;

    /// <inheritdoc/>
    public async Task<string?> GetConnectionStringAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await SecureStorage.Default.GetAsync(SecureKey(id));
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
            _onChanged?.Invoke(items.OrderBy(c => c.Name).ToList());
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
            _onChanged?.Invoke(items.OrderBy(c => c.Name).ToList());
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> Unsubscribe(Action<IReadOnlyList<DatabaseConnectionInfo>> action)
    {
        _onChanged -= action;
        return true;
    }

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
        await SecureStorage.Default.SetAsync(SecureKey(info.Id), connectionString);

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
            _onChanged?.Invoke(items.OrderBy(c => c.Name).ToList());
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
            _onChanged?.Invoke(items.OrderBy(c => c.Name).ToList());
        }
        finally
        {
            _gate.Release();
        }

        try
        {
            SecureStorage.Default.Remove(SecureKey(id));
        }
        catch (Exception)
        {
            // Le descripteur est parti : une entree residuelle du coffre est sans effet.
        }
    }

    private static string SecureKey(Guid id) => $"{SecureKeyPrefix}{id:N}";

    /// <summary>
    /// Fichier corrompu : copie mise de cote, puis liste vide (la reecriture ne perd plus rien).
    /// Fichier illisible (verrouille, droits) ou copie impossible : rien n'est mis en cache et une
    /// ecriture (<paramref name="forWrite"/>) est refusee, sinon elle effacerait les connexions enregistrees.
    /// </summary>
    private async Task<List<DatabaseConnectionInfo>> LoadUnsafeAsync(bool forWrite, CancellationToken cancellationToken)
    {
        if (_cache is not null)
            return _cache;

        if (!File.Exists(_filePath))
            return _cache = [];

        string json;
        try
        {
            json = await File.ReadAllTextAsync(_filePath, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Unreadable(forWrite, ex, $"Connexions illisibles : {ex.Message}");
        }

        try
        {
            return _cache = JsonConvert.DeserializeObject<List<DatabaseConnectionInfo>>(json) ?? [];
        }
        catch (JsonException ex)
        {
            var backupPath = CorruptFileHelper.Backup(_filePath);
            if (backupPath is null)
                return Unreadable(forWrite, ex, $"{FileName} est corrompu et n'a pas pu être copié : connexions non chargées.");

            WeakReferenceMessenger.Default.LogError(ex, $"{FileName} était corrompu : copie conservée sous {Path.GetFileName(backupPath)}.");
            return _cache = [];
        }
    }

    private static List<DatabaseConnectionInfo> Unreadable(bool forWrite, Exception exception, string message)
    {
        // En ecriture, l'appelant recoit l'erreur (RunAsBusyAsync ou popup la journalisent).
        if (forWrite)
            throw new IOException($"{message} Modification annulée pour ne pas écraser les connexions enregistrées.", exception);

        WeakReferenceMessenger.Default.LogError(exception, message);
        return [];
    }

    private async Task SaveUnsafeAsync(List<DatabaseConnectionInfo> items, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        // Ecriture atomique : un crash en cours d'ecriture ne laisse pas un JSON tronque.
        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            using var writer = new StreamWriter(stream);
            var json = JsonConvert.SerializeObject(items);
            await writer.WriteAsync(json);
        }

        File.Move(tempPath, _filePath, overwrite: true);
        _cache = items;
    }
}
