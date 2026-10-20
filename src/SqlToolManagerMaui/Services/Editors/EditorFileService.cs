using CommunityToolkit.Maui.Storage;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Helpers.Tools;

namespace SqlToolManagerMaui.Services.Editors;

/// <inheritdoc cref="IEditorFileService" />
[AutoRegister(As = typeof(IEditorFileService))]
public sealed class EditorFileService : IEditorFileService
{
    private static readonly FilePickerFileType TextFileTypes = new(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.WinUI] = [".sql", ".linq", ".schx", ".csx", ".txt", ".cs", ".json", ".xml", ".md", ".csv"],
            [DevicePlatform.macOS] = ["sql", "linq", "schx", "csx", "txt", "cs", "json", "xml", "md", "csv"],
            [DevicePlatform.MacCatalyst] = ["sql", "linq", "schx", "csx", "txt", "cs", "json", "xml", "md", "csv"],
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
