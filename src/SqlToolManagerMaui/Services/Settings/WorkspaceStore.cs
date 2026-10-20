using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Helpers.Tools;
using SqlToolManagerMaui.UI.Extensions;

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
            // Demarrer sans onglets restaures plutot que bloquer ; la copie garde les textes non enregistres
            // que la sauvegarde de fin de session ecrasera.
            var backupPath = CorruptFileHelper.Backup(_filePath);
            WeakReferenceMessenger.Default.LogError(ex, backupPath is null
                ? $"Onglets précédents non restaurés : {FileName} illisible."
                : $"Onglets précédents non restaurés : copie conservée sous {Path.GetFileName(backupPath)}.");
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
