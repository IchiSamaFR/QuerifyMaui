using Querify.Core.Enums.Editors.Completion;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Completion;
using Querify.Languages.Commons.Editors.Completion;
using Querify.Languages.Sql.Completion;
using Querify.Services.Editors.Highlighting;
using Quill.Code.Completion;
using System.Collections.Concurrent;

namespace Querify.Services.Editors.Completion;

/// <summary>
/// Identifiants deja ecrits dans le document (tables, colonnes, alias), comme les
/// suggestions "par mot" de VS Code : utile hors connexion et en complement du schema.
/// </summary>
[AutoRegister(As = typeof(ICompletionProvider))]
public sealed class DocumentWordCompletionProvider : ICompletionProvider
{
    private readonly ISyntaxConfigurationStore _configurations;

    /// <summary>Mots-cles par langage : construits une fois, et non a chaque ouverture de la liste.</summary>
    private readonly ConcurrentDictionary<SyntaxLanguage, HashSet<string>> _keywords = new();

    public DocumentWordCompletionProvider(ISyntaxConfigurationStore configurations)
    {
        _configurations = configurations;
    }

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (request.Language != SyntaxLanguage.Sql || context.Kind is CompletionContextKind.None or CompletionContextKind.DataType or CompletionContextKind.AfterTableName)
            return [];

        var keywords = await GetKeywordsAsync(request.Language, cancellationToken);
        return CollectWords(request, keywords);
    }

    private async Task<HashSet<string>> GetKeywordsAsync(SyntaxLanguage language, CancellationToken cancellationToken)
    {
        if (_keywords.TryGetValue(language, out var cached))
            return cached;

        var configuration = await _configurations.GetAsync(language, cancellationToken);
        var keywords = new HashSet<string>(configuration?.Definition.Keywords.Select(k => k.Word) ?? [], StringComparer.OrdinalIgnoreCase);
        return _keywords.GetOrAdd(language, keywords);
    }

    /// <summary>Le mot en cours de frappe est exclu : il se proposerait lui-meme.</summary>
    private static IReadOnlyList<CompletionItem> CollectWords(CompletionRequest request, HashSet<string> keywords)
        => SqlLexer.Tokenize(request.Text)
                   .Where(token => token.Kind is SqlTokenKind.Word or SqlTokenKind.QuotedIdentifier && token.Start != request.WordStart)
                   .Select(token => token.GetText(request.Text))
                   .Where(word => word.Length > 1 && !keywords.Contains(word))
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .Select(word => new CompletionItem { Label = word, Kind = CompletionItemKind.Text })
                   .ToArray();
}
