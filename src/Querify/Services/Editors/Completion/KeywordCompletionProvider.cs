using Querify.Core.Enums.Editors.Completion;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Completion;
using Querify.Core.Models.Editors.Highlighting;
using Querify.Databases.Models.Connections;
using Querify.Languages.Commons.Editors.Completion;
using Querify.Services.Editors.Highlighting;
using Quill.Code.Completion;
using System.Collections.Concurrent;

namespace Querify.Services.Editors.Completion;

/// <summary>Mots-cles, types et fonctions du langage, repris des listes du .xshd.</summary>
[AutoRegister(As = typeof(ICompletionProvider))]
public sealed class KeywordCompletionProvider : ICompletionProvider
{
    /// <summary>Seuls mots attendus apres un nom de table : la liste doit gener le moins possible la frappe d'un alias.</summary>
    private static readonly HashSet<string> FollowTableName = new(StringComparer.OrdinalIgnoreCase)
    {
        "AS", "WITH", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS", "APPLY", "ON",
        "GROUP", "ORDER", "HAVING", "UNION", "EXCEPT", "INTERSECT", "SET", "VALUES", "SELECT", "OUTPUT", "OFFSET"
    };

    private readonly ISyntaxConfigurationStore _configurations;
    // Concurrent : prechargement et demandes tournent maintenant hors du thread UI, en parallele.
    private readonly ConcurrentDictionary<SyntaxLanguage, IReadOnlyList<CompletionItem>> _cache = new();

    public KeywordCompletionProvider(ISyntaxConfigurationStore configurations)
    {
        _configurations = configurations;
    }

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // En LINQ, Roslyn propose deja les mots-cles C#, et au bon endroit.
        if (request.Language == SyntaxLanguage.Linq)
            return [];
        ArgumentNullException.ThrowIfNull(context);

        // Table, base, membre ou procedure : place reservee aux objets du schema.
        Func<CompletionItem, bool>? accepts = context.Kind switch
        {
            CompletionContextKind.General => static _ => true,
            CompletionContextKind.Column => static item => item.Kind != CompletionItemKind.DataType,
            CompletionContextKind.DataType => static item => item.Kind == CompletionItemKind.DataType,
            CompletionContextKind.AfterTableName => static item => FollowTableName.Contains(item.Label),
            _ => null
        };

        if (accepts is null)
            return [];

        return (await GetKeywordsAsync(request.Language, cancellationToken)).Where(accepts).ToArray();
    }

    public async Task WarmUpAsync(SyntaxLanguage language, DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        if (language != SyntaxLanguage.Linq)
            await GetKeywordsAsync(language, cancellationToken);
    }

    private async Task<IReadOnlyList<CompletionItem>> GetKeywordsAsync(SyntaxLanguage language, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(language, out var cached))
            return cached;

        var configuration = await _configurations.GetAsync(language, cancellationToken);
        var items = configuration?.Definition.Keywords.Select(ToItem).ToArray() ?? [];

        _cache[language] = items;
        return items;
    }

    private static CompletionItem ToItem(SyntaxKeyword keyword) => new()
    {
        Label = keyword.Word,
        Kind = keyword.Category switch
        {
            "Function" => CompletionItemKind.Function,
            "DataType" or "Type" => CompletionItemKind.DataType,
            _ => CompletionItemKind.Keyword,
        },
    };
}
