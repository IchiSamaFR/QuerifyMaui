using SqlToolManagerMaui.Core.Enums.Editors.Completion;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Editors.Completion;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Services.Editors.Completion;
using SqlToolManagerMaui.Languages.Commons.Editors.Completion;

namespace SqlToolManagerMaui.Services.Editors.Completion;

/// <inheritdoc cref="ICompletionService"/>
[AutoRegister(As = typeof(ICompletionService))]
public sealed class CompletionService : ICompletionService
{
    /// <summary>Au-dela, la liste n'aide plus : mieux vaut taper une lettre de plus.</summary>
    private const int MaxResults = 100;

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

        // Chaque fournisseur hors du thread UI : Roslyn ou un catalogue volumineux ont une partie synchrone
        // non negligeable avant leur premier await, qui figeait la frappe.
        var results = await Task.WhenAll(_providers.Select(provider => Task.Run(() => provider.GetItemsAsync(request, context, cancellationToken), cancellationToken)));

        return await Task.Run(() => Merge(results), cancellationToken);
    }

    public Task WarmUpAsync(SyntaxLanguage language, DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
        => Task.WhenAll(_providers.Select(provider => Task.Run(async () =>
        {
            try
            {
                await provider.WarmUpAsync(language, connection, cancellationToken);
            }
            catch (Exception)
            {
                // Prechargement opportuniste : la vraie demande retentera et gerera l'erreur.
            }
        }, CancellationToken.None)));

    private static CompletionItem[] Merge(IReadOnlyList<CompletionItem>[] results)
    {
        // MAX est a la fois type et fonction, Users peut exister dans deux schemas : seuls les vrais doublons partent.
        var seen = new HashSet<(string, CompletionItemKind, string?)>();
        var items = new List<CompletionItem>(results.Sum(batch => batch.Count));
        foreach (var item in results.SelectMany(batch => batch))
        {
            if (seen.Add((item.Label.ToUpperInvariant(), item.Kind, item.Detail)))
                items.Add(item);
        }

        // Un mot du document deja connu du schema ou des mots-cles ferait doublon, sans type ni icone.
        var known = items.Where(item => item.Kind != CompletionItemKind.Text)
                         .Select(item => item.Label)
                         .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return items.Where(item => item.Kind != CompletionItemKind.Text || !known.Contains(item.Label.Trim('[', ']')))
                    .ToArray();
    }

    /// <summary>Rejoue a chaque touche sur le thread UI : une passe, un tri en place, sans LINQ.</summary>
    public IReadOnlyList<CompletionItem> Filter(IReadOnlyList<CompletionItem> candidates, string prefix)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        prefix ??= string.Empty;

        var matches = new List<(CompletionItem Item, int Score)>(Math.Min(candidates.Count, 512));
        for (var i = 0; i < candidates.Count; i++)
        {
            var item = candidates[i];
            var score = Score(item.Label, prefix);
            if (score >= 0)
                matches.Add((item, score));
        }

        matches.Sort(static (a, b) =>
        {
            var result = a.Score.CompareTo(b.Score);
            if (result == 0)
                result = a.Item.Kind.CompareTo(b.Item.Kind);
            return result != 0 ? result : StringComparer.OrdinalIgnoreCase.Compare(a.Item.Label, b.Item.Label);
        });

        var count = Math.Min(matches.Count, MaxResults);
        var items = new CompletionItem[count];
        for (var i = 0; i < count; i++)
            items[i] = matches[i].Item;

        return items;
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
