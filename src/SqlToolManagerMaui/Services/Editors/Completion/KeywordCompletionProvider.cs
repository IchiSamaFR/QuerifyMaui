using SqlToolManagerMaui.Models.Editors.Completion;
using SqlToolManagerMaui.Models.Editors.Highlighting;
using SqlToolManagerMaui.Services.Editors.Highlighting;

namespace SqlToolManagerMaui.Services.Editors.Completion;

/// <summary>Mots-cles, types et fonctions du langage, repris des listes du .xshd.</summary>
[AutoRegister(As = typeof(ICompletionProvider))]
public sealed class KeywordCompletionProvider : ICompletionProvider
{
    private readonly ISyntaxConfigurationStore _configurations;
    private readonly Dictionary<SyntaxLanguage, IReadOnlyList<CompletionItem>> _cache = [];

    public KeywordCompletionProvider(ISyntaxConfigurationStore configurations)
    {
        _configurations = configurations;
    }

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        // Apres un point on attend un membre (colonne, table), jamais un mot-cle.
        if (request.Trigger == CompletionTrigger.MemberAccess)
            return [];

        if (_cache.TryGetValue(request.Language, out var cached))
            return cached;

        var configuration = await _configurations.GetAsync(request.Language, cancellationToken);
        var items = configuration?.Definition.Keywords.Select(ToItem).ToArray() ?? [];

        _cache[request.Language] = items;
        return items;
    }

    private static CompletionItem ToItem(SyntaxKeyword keyword) => new()
    {
        Label = keyword.Word,
        Kind = keyword.Category switch
        {
            "Function" => CompletionItemKind.Function,
            "DataType" => CompletionItemKind.DataType,
            _ => CompletionItemKind.Keyword,
        },
    };
}
