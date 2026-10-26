using CommunityToolkit.Mvvm.Messaging;
using Querify.Core.Ioc;
using Querify.Core.Models.Settings;
using Querify.Helpers.Tools;
using Querify.UI.Extensions;

namespace Querify.Services.Settings;

/// <inheritdoc cref="IWorkspaceStore"/>
[AutoRegister(As = typeof(IWorkspaceStore))]
public sealed class WorkspaceStore : IWorkspaceStore
{
    private const string FileName = "workspace.json";

    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);

    /// <inheritdoc/>
    public async Task<WorkspaceState?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var result = await JsonFile.ReadAsync<WorkspaceState>(_filePath, cancellationToken);
        if (result.Status is JsonFileStatus.Missing or JsonFileStatus.Loaded)
            return result.Value;

        // Demarrer sans onglets restaures plutot que bloquer ; la copie garde les textes non enregistres
        // que la sauvegarde de fin de session ecrasera.
        var backupPath = result.BackupPath ?? CorruptFileHelper.Backup(_filePath);
        WeakReferenceMessenger.Default.LogError(result.Error!, backupPath is null
            ? $"Onglets précédents non restaurés : {FileName} illisible."
            : $"Onglets précédents non restaurés : copie conservée sous {Path.GetFileName(backupPath)}.");
        return null;
    }

    /// <inheritdoc/>
    public void Save(WorkspaceState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        JsonFile.WriteAllText(_filePath, JsonFile.Serialize(state));
    }

    /// <inheritdoc/>
    public void Clear()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }
}
