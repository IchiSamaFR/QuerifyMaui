using SqlToolManagerMaui.Models.Editors.Completion;
using SqlToolManagerMaui.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Services.Editors.Completion;

/// <inheritdoc cref="ICompletionService"/>
[AutoRegister(As = typeof(ICompletionService))]
public sealed class CompletionService : ICompletionService
{
    /// <summary>Au-dela, la liste n'aide plus : mieux vaut taper une lettre de plus.</summary>
    private const int MaxResults = 50;

    private readonly IReadOnlyList<ICompletionProvider> _providers;
    private readonly Dictionary<SyntaxLanguage, ICompletionContextAnalyzer> _analyzers;

    public CompletionService(IEnumerable<ICompletionProvider> providers, IEnumerable<ICompletionContextAnalyzer> analyzers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(analyzers);

        _providers = providers.ToArray();
        _analyzers = analyzers.ToDictionary(a => a.Language);
    }

    public async Task<IReadOnlyList<CompletionItem>> GetCandidatesAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Langage sans analyseur : on propose tout, comme avant l'analyse de contexte.
        var context = _analyzers.TryGetValue(request.Language, out var analyzer)
            ? await Task.Run(() => analyzer.Analyze(request), cancellationToken)
            : CompletionContext.General;

        if (context.Kind == CompletionContextKind.None)
            return [];

        var results = await Task.WhenAll(_providers.Select(provider => provider.GetItemsAsync(request, context, cancellationToken)));

        // MAX est a la fois type et fonction, Users peut exister dans deux schemas : seuls les vrais doublons partent.
        var items = results.SelectMany(batch => batch)
                           .DistinctBy(item => (item.Label.ToUpperInvariant(), item.Kind, item.Detail))
                           .ToArray();

        // Un mot du document deja connu du schema ou des mots-cles ferait doublon, sans type ni icone.
        var known = items.Where(item => item.Kind != CompletionItemKind.Text)
                         .Select(item => item.Label)
                         .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return items.Where(item => item.Kind != CompletionItemKind.Text || !known.Contains(item.Label.Trim('[', ']')))
                    .ToArray();
    }

    public IReadOnlyList<CompletionItem> Filter(IReadOnlyList<CompletionItem> candidates, string prefix)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates.Select(item => (Item: item, Score: Score(item.Label, prefix ?? string.Empty)))
                         .Where(match => match.Score >= 0)
                         .OrderBy(match => match.Score)
                         .ThenBy(match => match.Item.Kind)
                         .ThenBy(match => match.Item.Label, StringComparer.OrdinalIgnoreCase)
                         .Take(MaxResults)
                         .Select(match => match.Item)
                         .ToArray();
    }

    /// <summary>0 = commence par, 1 = debut d'un segment (NUM -> ROW_NUMBER), 2 = contient, -1 = rejete.</summary>
    private static int Score(string label, string prefix)
    {
        if (prefix.Length == 0 || label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 0;

        var index = label.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return -1;

        return label[index - 1] == '_' ? 1 : 2;
    }
}
