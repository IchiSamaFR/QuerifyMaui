using SqlToolManagerMaui.Models.Editors.Completion;

namespace SqlToolManagerMaui.Services.Editors.Completion;

/// <inheritdoc cref="ICompletionService"/>
[AutoRegister(As = typeof(ICompletionService))]
public sealed class CompletionService : ICompletionService
{
    /// <summary>Au-dela, la liste n'aide plus : mieux vaut taper une lettre de plus.</summary>
    private const int MaxResults = 50;

    private readonly IReadOnlyList<ICompletionProvider> _providers;

    public CompletionService(IEnumerable<ICompletionProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers.ToArray();
    }

    public async Task<IReadOnlyList<CompletionItem>> GetCandidatesAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(_providers.Select(provider => provider.GetItemsAsync(request, cancellationToken)));

        // MAX est a la fois un type et une fonction : on garde les deux, mais pas deux fois le meme.
        return results.SelectMany(items => items)
                      .DistinctBy(item => (item.Label.ToUpperInvariant(), item.Kind))
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
