using Newtonsoft.Json;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Helpers.Extensions;
using System.ComponentModel;

namespace SqlToolManagerMaui.Services.Settings;

/// <inheritdoc cref="IUserPreferencesStore"/>
[AutoRegister(As = typeof(IUserPreferencesStore))]
public sealed class UserPreferencesStore : IUserPreferencesStore
{
    private const string FileName = "preferences.json";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);

    private bool _isLoaded;

    /// <inheritdoc/>
    public UserPreferences Preferences { get; } = new();

    /// <inheritdoc/>
    public event EventHandler<Exception>? SaveFailed;

    /// <inheritdoc/>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded)
            return;

        // Fusion plutot que remplacement : les vues restent liees aux memes instances.
        if (await ReadAsync(cancellationToken) is { } stored)
        {
            Preferences.MergeFrom(stored);
            Preferences.Editor.MergeFrom(stored.Editor);
            Preferences.Panels.MergeFrom(stored.Panels);
        }

        // Abonnement apres la fusion : le chargement ne doit pas reecrire le fichier.
        Preferences.PreferencesChanged += OnPreferencesChanged;
        _isLoaded = true;
    }

    private async Task<UserPreferences?> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<UserPreferences>(await File.ReadAllTextAsync(_filePath, cancellationToken));
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Fichier corrompu ou verrouille : les valeurs par defaut valent mieux qu'un demarrage bloque.
            return null;
        }
    }

    private async void OnPreferencesChanged()
    {
        // Copie avant l'attente : les modifications suivantes ne doivent pas se meler a cette ecriture.
        var json = JsonConvert.SerializeObject(Preferences, Formatting.Indented);

        await _gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            // Ecriture atomique : un crash en cours d'ecriture ne laisse pas un JSON tronque.
            var tempPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SaveFailed?.Invoke(this, ex);
        }
        finally
        {
            _gate.Release();
        }
    }
}
