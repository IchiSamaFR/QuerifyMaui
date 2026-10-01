using Newtonsoft.Json;
using SqlToolManagerMaui.Models;
using SqlToolManagerMaui.Models.Databases;
using SqlToolManagerMaui.UI.Extensions;

namespace SqlToolManagerMaui.Services.Databases;

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

    public async Task<IReadOnlyList<DatabaseConnectionInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await LoadUnsafeAsync(cancellationToken);
            return items
                .Select(i => i.Clone())
                .OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

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

    public async Task SaveAsync(DatabaseConnectionInfo info, string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Le secret d'abord : si le coffre refuse, aucun descripteur orphelin n'est ecrit.
        await SecureStorage.Default.SetAsync(SecureKey(info.Id), connectionString);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await LoadUnsafeAsync(cancellationToken);
            var index = items.FindIndex(i => i.Id == info.Id);

            info.UpdatedAt = DateTimeOffset.Now;
            if (index >= 0)
            {
                info.CreatedAt = items[index].CreatedAt;
                items[index] = info.Clone();
            }
            else
            {
                items.Add(info.Clone());
            }

            await SaveUnsafeAsync(items, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = await LoadUnsafeAsync(cancellationToken);
            if (items.RemoveAll(i => i.Id == id) > 0)
            {
                await SaveUnsafeAsync(items, cancellationToken);
            }
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

    private async Task<List<DatabaseConnectionInfo>> LoadUnsafeAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
            return _cache;

        if (!File.Exists(_filePath))
            return _cache = [];

        try
        {
            await using var stream = File.OpenRead(_filePath);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            var items = JsonConvert.DeserializeObject<List<DatabaseConnectionInfo>>(json);

            return _cache = items ?? [];
        }
        catch (JsonException)
        {
            // Fichier corrompu : on repart d'une liste vide plutot que de bloquer la page.
            return _cache = [];
        }
        catch (IOException)
        {
            return _cache = [];
        }
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
