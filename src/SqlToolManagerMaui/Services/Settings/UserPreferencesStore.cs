using Newtonsoft.Json;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Settings;

namespace SqlToolManagerMaui.Services.Settings;

/// <inheritdoc cref="IUserPreferencesStore"/>
[AutoRegister(As = typeof(IUserPreferencesStore))]
public sealed class UserPreferencesStore : IUserPreferencesStore
{
    private const string FileName = "preferences.json";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);

    private UserPreferences? _cache;

    /// <inheritdoc/>
    public async Task<UserPreferences> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return (await LoadUnsafeAsync()).Clone();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<UserPreferences> UpdateAsync(Action<UserPreferences> update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var preferences = await LoadUnsafeAsync();
            update(preferences);
            await SaveUnsafeAsync(preferences);
            return preferences.Clone();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<UserPreferences> LoadUnsafeAsync()
    {
        if (_cache is not null)
            return _cache;

        if (!File.Exists(_filePath))
            return _cache = new();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return _cache = JsonConvert.DeserializeObject<UserPreferences>(json) ?? new();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Fichier corrompu ou verrouille : les valeurs par defaut valent mieux qu'un demarrage bloque.
            return _cache = new();
        }
    }

    private async Task SaveUnsafeAsync(UserPreferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        // Ecriture atomique : un crash en cours d'ecriture ne laisse pas un JSON tronque.
        var tempPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, JsonConvert.SerializeObject(preferences, Formatting.Indented));
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
