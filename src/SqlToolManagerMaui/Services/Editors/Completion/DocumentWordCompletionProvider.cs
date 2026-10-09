using SqlToolManagerMaui.Core.Enums.Editors.Completion;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Editors.Completion;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Services.Editors.Completion.Sql;
using SqlToolManagerMaui.Services.Editors.Highlighting;

namespace SqlToolManagerMaui.Services.Editors.Completion;

/// <summary>
/// Identifiants deja ecrits dans le document (tables, colonnes, alias), comme les
/// suggestions "par mot" de VS Code : utile hors connexion et en complement du schema.
/// </summary>
[AutoRegister(As = typeof(ICompletionProvider))]
public sealed class DocumentWordCompletionProvider : ICompletionProvider
{
    private readonly ISyntaxConfigurationStore _configurations;

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

        var configuration = await _configurations.GetAsync(request.Language, cancellationToken);
        var keywords = new HashSet<string>(configuration?.Definition.Keywords.Select(k => k.Word) ?? [], StringComparer.OrdinalIgnoreCase);

        return await Task.Run(() => CollectWords(request, keywords), cancellationToken);
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
