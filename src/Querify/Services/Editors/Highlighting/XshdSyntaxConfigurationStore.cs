using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Highlighting;
using Quill.Code.Highlighting;

namespace Querify.Services.Editors.Highlighting;

/// <summary>
/// Charge les fichiers .xshd (format AvalonEdit) embarques dans les assets et les lit avec Quill.
/// </summary>
[AutoRegister(As = typeof(ISyntaxConfigurationStore))]
public sealed class XshdSyntaxConfigurationStore : ISyntaxConfigurationStore
{
    private const string AssetFolder = "SyntaxHighlights";

    /// <summary>Prefixe de commentaire quand le .xshd n'en declare pas.</summary>
    private const string DefaultLineComment = "--";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<SyntaxLanguage, SyntaxConfiguration?> _cache = [];

    public async Task<SyntaxConfiguration?> GetAsync(SyntaxLanguage language, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(language, out var cached))
                return cached;

            var configuration = await LoadAsync(language, cancellationToken);
            _cache[language] = configuration;
            return configuration;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SyntaxLanguage?> FindLanguageAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension))
            return null;

        foreach (var language in Enum.GetValues<SyntaxLanguage>().Where(l => l != SyntaxLanguage.None))
        {
            var configuration = await GetAsync(language, cancellationToken);
            if (configuration?.Definition.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase) == true)
                return language;
        }

        return null;
    }

    private static async Task<SyntaxConfiguration?> LoadAsync(SyntaxLanguage language, CancellationToken cancellationToken)
    {
        var assetPath = $"{AssetFolder}/{language}.xshd";

        if (language == SyntaxLanguage.None || !await FileSystem.AppPackageFileExistsAsync(assetPath))
            return null;

        await using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
        var definition = await XshdReader.LoadAsync(stream, cancellationToken);

        return new SyntaxConfiguration
        {
            Language = language,
            Comments = definition.LineComment ?? DefaultLineComment,
            Definition = definition,
        };
    }
}
