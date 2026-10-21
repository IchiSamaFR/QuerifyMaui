using CommunityToolkit.Maui.Storage;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Helpers.Tools;
using System.Diagnostics;

namespace SqlToolManagerMaui.Services.Editors;

/// <inheritdoc cref="IEditorFileService" />
[AutoRegister(As = typeof(IEditorFileService))]
public sealed class EditorFileService : IEditorFileService
{
    private static readonly FilePickerFileType TextFileTypes = new(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.WinUI] = [".sql", ".linq", ".schx", ".csx", ".txt", ".cs", ".json", ".xml", ".md", ".csv"],
        });

    public async Task<EditorFileContent?> OpenAsync(CancellationToken cancellationToken = default)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Ouvrir un fichier",
            FileTypes = TextFileTypes,
        });

        if (result is null)
            return null;

        return await ReadAsync(result.FullPath, cancellationToken);
    }

    public async Task<EditorFileContent?> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return new EditorFileContent(filePath, TextFileEncoding.Decode(bytes, TextFileEncoding.Detect(bytes)));
    }

    public async Task SaveAsync(string filePath, string text, CancellationToken cancellationToken = default)
    {
        // Encodage du fichier tel qu'il est sur le disque : l'enregistrer ne doit pas le convertir.
        var encoding = File.Exists(filePath)
            ? TextFileEncoding.Detect(await File.ReadAllBytesAsync(filePath, cancellationToken))
            : TextFileEncoding.Default;

        // Ecriture atomique : un arret en cours d'ecriture ne laisse pas le script tronque.
        var tempPath = filePath + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(tempPath, TextFileEncoding.Encode(text, encoding), cancellationToken);

            // Replace garde les attributs et la date de creation du fichier d'origine.
            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, destinationBackupFileName: null);
            else
                File.Move(tempPath, filePath);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    public async Task<string?> SaveAsAsync(string suggestedFileName, string text, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(TextFileEncoding.Encode(text, TextFileEncoding.Default));
        var result = await FileSaver.Default.SaveAsync(suggestedFileName, stream, cancellationToken);

        return result.IsSuccessful ? result.FilePath : null;
    }

    public Task<string> RenameAsync(string filePath, string newFileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(newFileName);

        var target = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, newFileName);

        return Task.Run(() =>
        {
            // Changement de casse seul (requete.sql -> Requete.sql) : la cible "existe", c'est le meme fichier.
            if (File.Exists(target) && !string.Equals(Path.GetFullPath(target), Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase))
                throw new IOException($"{newFileName} existe déjà dans ce dossier.");

            File.Move(filePath, target);
            return target;
        }, cancellationToken);
    }

    public bool RevealInExplorer(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        // /select, : le dossier s'ouvre avec le fichier deja selectionne.
        using var explorer = Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"") { UseShellExecute = true });
        return true;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Fichier temporaire residuel : sans effet, ecrase au prochain enregistrement.
        }
    }
}
