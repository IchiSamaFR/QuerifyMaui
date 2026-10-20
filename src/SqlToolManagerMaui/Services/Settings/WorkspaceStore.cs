using Newtonsoft.Json;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Settings;

namespace SqlToolManagerMaui.Services.Settings;

/// <inheritdoc cref="IWorkspaceStore"/>
[AutoRegister(As = typeof(IWorkspaceStore))]
public sealed class WorkspaceStore : IWorkspaceStore
{
    private const string FileName = "workspace.json";

    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);

    /// <inheritdoc/>
    public async Task<WorkspaceState?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            return JsonConvert.DeserializeObject<WorkspaceState>(json);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Fichier corrompu ou verrouille : demarrer sans onglets restaures plutot que bloquer.
            return null;
        }
    }

    /// <inheritdoc/>
    public void Save(WorkspaceState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        // Ecriture atomique : un arret en cours d'ecriture ne laisse pas un JSON tronque.
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, JsonConvert.SerializeObject(state, Formatting.Indented));
        File.Move(tempPath, _filePath, overwrite: true);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }
}
