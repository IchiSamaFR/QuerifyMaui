using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Languages.Linq.Compilation;
using SqlToolManagerMaui.Languages.Linq.Queries;
using SqlToolManagerMaui.Languages.Linq.Workspaces;
using System.Collections.Immutable;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace SqlToolManagerMaui.Languages.Linq.Completion;

/// <inheritdoc cref="ILinqCompletionEngine"/>
[AutoRegister(As = typeof(ILinqCompletionEngine))]
public sealed class LinqCompletionEngine : ILinqCompletionEngine
{
    /// <summary>Tags Roslyn retenus pour la nature, du plus precis au plus general.</summary>
    private static readonly string[] KindTags =
    [
        "ExtensionMethod", "Method", "Property", "Field", "Event", "Local", "Parameter", "RangeVariable",
        "Class", "Structure", "Interface", "Enum", "EnumMember", "Delegate", "Namespace", "Keyword", "Snippet"
    ];

    private readonly ILinqWorkspace _workspace;

    public LinqCompletionEngine(ILinqWorkspace workspace)
    {
        _workspace = workspace;
    }

    public async Task<IReadOnlyList<LinqCompletionCandidate>> GetCompletionsAsync(LinqModelAssembly? model, string query, int caretOffset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var linq = _workspace.GetDocument(model, query, parameters);
        var document = linq.Document;
        if (RoslynCompletion.CompletionService.GetService(document) is not { } service)
            return [];

        var position = linq.QueryStart + Math.Clamp(caretOffset, 0, query.Length);
        var list = await service.GetCompletionsAsync(document, position, cancellationToken: cancellationToken);
        var source = (await document.GetTextAsync(cancellationToken)).ToString();

        // Roslyn propose tout ce qui est visible (System.Security, System.IO, snippets...) :
        // on ne garde que ce qui sert a ecrire une requete EF.
        var filter = new LinqCompletionFilter(model?.Model, MemberAccessTarget(source, list.Span.Start));

        return list.ItemsList
            .Where(item => filter.Keeps(item.DisplayText, KindOf(item.Tags)))
            .Select(item => new LinqCompletionCandidate(
                item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix,
                item.DisplayText,
                KindOf(item.Tags),
                string.IsNullOrEmpty(item.InlineDescription) ? null : item.InlineDescription))
            .ToArray();
    }

    /// <summary>
    /// Expression avant le "." qui precede le mot complete : son dernier identifiant ("RaveOnDb", "u"),
    /// chaine vide si c'est autre chose (appel, indexeur) ; null hors acces membre.
    /// </summary>
    private static string? MemberAccessTarget(string source, int wordStart)
    {
        var i = wordStart - 1;
        while (i >= 0 && char.IsWhiteSpace(source[i]))
            i--;

        if (i < 0 || source[i] != '.')
            return null;

        i--;
        while (i >= 0 && char.IsWhiteSpace(source[i]))
            i--;

        var end = i + 1;
        while (i >= 0 && (char.IsLetterOrDigit(source[i]) || source[i] == '_'))
            i--;

        return source[(i + 1)..end];
    }

    private static string KindOf(ImmutableArray<string> tags)
        => KindTags.FirstOrDefault(tags.Contains) ?? (tags.IsDefaultOrEmpty ? "Text" : tags[0]);
}
