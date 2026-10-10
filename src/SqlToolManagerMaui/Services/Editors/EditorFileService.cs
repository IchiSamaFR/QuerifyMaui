using CommunityToolkit.Maui.Storage;
using SqlToolManagerMaui.Core.Ioc;
using System.Text;

namespace SqlToolManagerMaui.Services.Editors;

/// <inheritdoc cref="IEditorFileService" />
[AutoRegister(As = typeof(IEditorFileService))]
public sealed class EditorFileService : IEditorFileService
{
    // Avec BOM : sans lui, Notepad devine l'encodage et lit les accents en ANSI.
    private static readonly UTF8Encoding Utf8Bom = new(encoderShouldEmitUTF8Identifier: true);

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

        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        return new EditorFileContent(filePath, text);
    }

    public Task SaveAsync(string filePath, string text, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(filePath, text, Utf8Bom, cancellationToken);

    public async Task<string?> SaveAsAsync(string suggestedFileName, string text, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream([.. Utf8Bom.GetPreamble(), .. Utf8Bom.GetBytes(text)]);
        var result = await FileSaver.Default.SaveAsync(suggestedFileName, stream, cancellationToken);

        return result.IsSuccessful ? result.FilePath : null;
    }
}
