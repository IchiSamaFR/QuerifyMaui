using Querify.Core.Services.Editors.Highlighting;
using CommunityToolkit.Mvvm.Messaging;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Enums;
using Querify.Services.Editors.Highlighting;
using Querify.UI.Extensions;
using System.Collections.Concurrent;

namespace Querify.Services.Editors;

[AutoRegister(As = typeof(IDocumentTemplateStore))]
public sealed class DocumentTemplateStore : IDocumentTemplateStore
{
    private const string AssetFolder = "Templates";

    private readonly ISyntaxConfigurationStore _syntaxConfigurations;
    private readonly ConcurrentDictionary<(SyntaxLanguage Language, DatabaseProvider? Provider), string> _cache = new();

    public DocumentTemplateStore(ISyntaxConfigurationStore syntaxConfigurations)
    {
        _syntaxConfigurations = syntaxConfigurations;
    }

    public async Task<string> GetAsync(SyntaxLanguage language, DatabaseProvider? provider, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue((language, provider), out var cached))
            return cached;

        var template = await LoadAsync(language, provider, cancellationToken);
        _cache[(language, provider)] = template;
        return template;
    }

    /// <summary>Le modele porte la premiere extension du .xshd : il s'ouvre tel quel dans un IDE.</summary>
    private async Task<string> LoadAsync(SyntaxLanguage language, DatabaseProvider? provider, CancellationToken cancellationToken)
    {
        var configuration = await _syntaxConfigurations.GetAsync(language, cancellationToken);
        if (configuration is not { Definition.Extensions: [var extension, ..] })
            return string.Empty;

        // Modele du moteur d'abord (TOP, LIMIT, tables du catalogue... different), sinon celui du langage ;
        // sans connexion, SQL Server comme le dialecte de l'editeur.
        var assetPath = $"{AssetFolder}/{language}.{provider ?? DatabaseProvider.SqlServer}{extension}";
        try
        {
            if (!await FileSystem.AppPackageFileExistsAsync(assetPath))
            {
                assetPath = $"{AssetFolder}/{language}{extension}";
                if (!await FileSystem.AppPackageFileExistsAsync(assetPath))
                    return string.Empty;
            }

            await using var stream = await FileSystem.OpenAppPackageFileAsync(assetPath);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (IOException ex)
        {
            // Un modele illisible ne doit pas empecher d'ouvrir un onglet vide.
            WeakReferenceMessenger.Default.LogError(ex, $"Modèle {assetPath} illisible : {ex.Message}");
            return string.Empty;
        }
    }
}
