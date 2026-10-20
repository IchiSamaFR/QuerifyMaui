using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Helpers.Extensions;
using SqlToolManagerMaui.Helpers.Tools;
using SqlToolManagerMaui.UI.Extensions;

namespace SqlToolManagerMaui.Services.Settings;

/// <inheritdoc cref="IUserPreferencesStore"/>
[AutoRegister(As = typeof(IUserPreferencesStore))]
public sealed class UserPreferencesStore : IUserPreferencesStore
{
    private const string FileName = "preferences.json";

    /// <summary>Regroupe les modifications rapprochees (bascules de menu, fichiers recents) en une seule ecriture.</summary>
    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(300);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);

    private bool _isLoaded;

    /// <summary>Fichier present mais non relu (verrouille, corrompu sans copie possible) : jamais reecrit pendant la session.</summary>
    private bool _isReadOnly;
    private bool _isReadOnlyReported;

    /// <summary>Derniere modification demandee et derniere ecrite : une ecriture plus ancienne n'ecrase jamais une plus recente.</summary>
    private int _requestedVersion;
    private int _writtenVersion;

    /// <inheritdoc/>
    public UserPreferences Preferences { get; } = new();

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

    /// <inheritdoc/>
    public void Flush()
    {
        var version = Volatile.Read(ref _requestedVersion);
        if (_isReadOnly || version <= Volatile.Read(ref _writtenVersion))
            return;

        try
        {
            var json = Serialize();
            _gate.Wait();
            try
            {
                if (version <= _writtenVersion)
                    return;

                WriteFile(json);
                _writtenVersion = version;
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (Exception ex)
        {
            // Fenetre en cours de fermeture : seul le journal peut encore le signaler.
            WeakReferenceMessenger.Default.LogError(ex, $"Préférences non enregistrées à la fermeture : {ex.Message}");
        }
    }

    private async Task<UserPreferences?> ReadAsync(CancellationToken cancellationToken)
    {
        var result = await JsonFile.ReadAsync<UserPreferences>(_filePath, cancellationToken);
        switch (result.Status)
        {
            case JsonFileStatus.Missing:
            case JsonFileStatus.Loaded:
                return result.Value;
            case JsonFileStatus.Corrupted:
                // Valeurs par defaut ; sans copie, l'ancien fichier n'est jamais reecrit.
                _isReadOnly = result.BackupPath is null;
                WeakReferenceMessenger.Default.LogError(result.Error!, result.BackupPath is null
                    ? $"{FileName} corrompu : préférences par défaut, fichier conservé tel quel."
                    : $"{FileName} corrompu : préférences par défaut, copie conservée sous {Path.GetFileName(result.BackupPath)}.");
                return null;
            default:
                // Fichier verrouille : le reecrire avec les valeurs par defaut ferait perdre les preferences.
                _isReadOnly = true;
                WeakReferenceMessenger.Default.LogError(result.Error!, $"Préférences illisibles : {result.Error!.Message}");
                return null;
        }
    }

    private void OnPreferencesChanged() => _ = SaveLaterAsync(Interlocked.Increment(ref _requestedVersion));

    /// <summary>Aucune exception ne s'en echappe : elle est journalisee (barre d'etat et fichier).</summary>
    private async Task SaveLaterAsync(int version)
    {
        try
        {
            await Task.Delay(SaveDelay);

            // Une modification plus recente a programme sa propre ecriture.
            if (version != Volatile.Read(ref _requestedVersion))
                return;

            await WriteAsync(version);
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.LogError(ex, $"Préférences non enregistrées : {ex.Message}");
        }
    }

    private async Task WriteAsync(int version)
    {
        if (_isReadOnly)
        {
            if (_isReadOnlyReported)
                return;

            _isReadOnlyReported = true;
            throw new IOException($"{FileName} etait illisible au demarrage : modifications non enregistrees pour ne pas l'ecraser.");
        }

        // Serialise sur le thread appelant (UI), comme les modifications : contenu au moins aussi recent que la version.
        var json = Serialize();

        // ConfigureAwait(false) : Flush peut bloquer le thread UI sur ce verrou pendant la fermeture.
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (version <= _writtenVersion)
                return;

            WriteFile(json);
            _writtenVersion = version;
        }
        finally
        {
            _gate.Release();
        }
    }

    private string Serialize() => JsonFile.Serialize(Preferences);

    private void WriteFile(string json) => JsonFile.WriteAllText(_filePath, json);
}
