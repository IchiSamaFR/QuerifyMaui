using CommunityToolkit.Maui.Storage;
using System.Text;

namespace SqlToolManagerMaui.Services.Editors;

/// <inheritdoc cref="IEditorFileService" />
[AutoRegister(As = typeof(IEditorFileService))]
public sealed class EditorFileService : IEditorFileService
{
    // UTF-8 sans BOM : SQL Server Management Studio et les outils en ligne de commande
    // n'aiment pas la marque d'ordre sur un .sql.
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly FilePickerFileType TextFileTypes = new(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.WinUI] = [".sql", ".linq", ".csx", ".txt", ".cs", ".json", ".xml", ".md", ".csv"],
            [DevicePlatform.macOS] = ["sql", "linq", "csx", "txt", "cs", "json", "xml", "md", "csv"],
            [DevicePlatform.MacCatalyst] = ["sql", "linq", "csx", "txt", "cs", "json", "xml", "md", "csv"],
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

        var text = await File.ReadAllTextAsync(result.FullPath, cancellationToken);
        return new EditorFileContent(result.FullPath, text);
    }

    public Task SaveAsync(string filePath, string text, CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(filePath, text, Utf8NoBom, cancellationToken);

    public async Task<string?> SaveAsAsync(string suggestedFileName, string text, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(Utf8NoBom.GetBytes(text));
        var result = await FileSaver.Default.SaveAsync(suggestedFileName, stream, cancellationToken);

        return result.IsSuccessful ? result.FilePath : null;
    }
}
