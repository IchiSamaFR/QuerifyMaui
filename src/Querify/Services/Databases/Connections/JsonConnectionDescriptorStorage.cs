using CommunityToolkit.Mvvm.Messaging;
using Querify.Core.Ioc;
using Querify.Databases.Connections;
using Querify.Databases.Models.Connections;
using Querify.Helpers.Tools;
using Querify.Services.Storage;
using Querify.UI.Extensions;

namespace Querify.Services.Databases.Connections;

/// <summary>Descripteurs de connexion dans connections.json (AppPaths.DataDirectory), sans chaine de connexion.</summary>
[AutoRegister(As = typeof(IConnectionDescriptorStorage))]
public sealed class JsonConnectionDescriptorStorage : IConnectionDescriptorStorage
{
    private const string FileName = "connections.json";

    private readonly string _filePath = Path.Combine(AppPaths.DataDirectory, FileName);

    /// <inheritdoc/>
    public async Task<List<DatabaseConnectionInfo>?> ReadAsync(CancellationToken cancellationToken = default)
    {
        var result = await JsonFile.ReadAsync<List<DatabaseConnectionInfo>>(_filePath, cancellationToken);
        switch (result.Status)
        {
            case JsonFileStatus.Missing:
                return [];
            case JsonFileStatus.Loaded:
                return result.Value ?? [];
            case JsonFileStatus.Corrupted when result.BackupPath is not null:
                // La copie mise de cote permet de repartir d'une liste vide sans rien perdre.
                WeakReferenceMessenger.Default.LogError(result.Error!, $"{FileName} était corrompu : copie conservée sous {Path.GetFileName(result.BackupPath)}.");
                return [];
            case JsonFileStatus.Corrupted:
                WeakReferenceMessenger.Default.LogError(result.Error!, $"{FileName} est corrompu et n'a pas pu être copié : connexions non chargées.");
                return null;
            default:
                WeakReferenceMessenger.Default.LogError(result.Error!, $"Connexions illisibles : {result.Error!.Message}");
                return null;
        }
    }

    /// <inheritdoc/>
    public Task WriteAsync(IReadOnlyList<DatabaseConnectionInfo> items, CancellationToken cancellationToken = default)
        => JsonFile.WriteAllTextAsync(_filePath, JsonFile.Serialize(items), cancellationToken);
}
