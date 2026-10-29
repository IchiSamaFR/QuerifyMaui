using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Services.Editors.Highlighting;
using Quill.Code.Completion;
using Quill.Code.Highlighting;

namespace Querify.Languages.Sql.Completion;

/// <summary>Mots-cles, types et fonctions T-SQL, repris des listes du .xshd et charges une fois.</summary>
internal sealed class SqlKeywordCatalog
{
    private readonly ISyntaxConfigurationStore _configurations;
    private Entries? _entries;

    public SqlKeywordCatalog(ISyntaxConfigurationStore configurations)
    {
        _configurations = configurations;
    }

    /// <summary>Mots du langage en propositions.</summary>
    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CancellationToken cancellationToken)
        => (await LoadAsync(cancellationToken)).Items;

    /// <summary>Tous les mots du langage : un mot du document qui en fait partie n'est pas propose une seconde fois.</summary>
    public async Task<IReadOnlySet<string>> GetWordsAsync(CancellationToken cancellationToken)
        => (await LoadAsync(cancellationToken)).Words;

    /// <summary>Seuls les mots-cles reserves : une colonne Year ou Date n'a pas besoin de crochets.</summary>
    public async Task<IReadOnlySet<string>> GetReservedWordsAsync(CancellationToken cancellationToken)
        => (await LoadAsync(cancellationToken)).Reserved;

    private async Task<Entries> LoadAsync(CancellationToken cancellationToken)
    {
        if (_entries is { } loaded)
            return loaded;

        var configuration = await _configurations.GetAsync(SyntaxLanguage.Sql, cancellationToken);
        var keywords = configuration?.Definition.Keywords ?? [];

        return _entries = new Entries(
            keywords.Select(ToItem).ToArray(),
            new HashSet<string>(keywords.Select(k => k.Word), StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(keywords.Where(k => k.Category == "Keyword").Select(k => k.Word), StringComparer.OrdinalIgnoreCase));
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

    private sealed record Entries(IReadOnlyList<CompletionItem> Items, IReadOnlySet<string> Words, IReadOnlySet<string> Reserved);
}
