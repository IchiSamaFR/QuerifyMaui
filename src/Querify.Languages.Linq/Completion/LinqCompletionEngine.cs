using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Tags;
using Querify.Core.Ioc;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using System.Collections.Immutable;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Querify.Languages.Linq.Completion;

/// <inheritdoc cref="ILinqCompletionEngine"/>
[AutoRegister(As = typeof(ILinqCompletionEngine))]
public sealed class LinqCompletionEngine : ILinqCompletionEngine
{
    /// <summary>Tags Roslyn retenus pour la nature, du plus precis au plus general.</summary>
    private static readonly string[] KindTags =
    [
        WellKnownTags.ExtensionMethod, WellKnownTags.Method, WellKnownTags.Property, WellKnownTags.Field, WellKnownTags.Event,
        WellKnownTags.Local, WellKnownTags.Parameter, WellKnownTags.RangeVariable,
        WellKnownTags.Class, WellKnownTags.Structure, WellKnownTags.Interface, WellKnownTags.Enum, WellKnownTags.EnumMember,
        WellKnownTags.Delegate, WellKnownTags.Namespace, WellKnownTags.Keyword, WellKnownTags.Snippet
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

        var imported = await ImportedNamespacesAsync(document, cancellationToken);

        // Toutes les propositions de Roslyn : le filtre de saisie de l'editeur les reduit a la frappe.
        List<LinqCompletionCandidate> candidates = [];
        foreach (var item in list.ItemsList)
        {
            var kind = KindOf(item.Tags);
            if (InsertTextOf(item, kind, imported) is not { } insertText)
                continue;

            candidates.Add(new LinqCompletionCandidate(
                item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix,
                insertText,
                kind,
                string.IsNullOrEmpty(item.InlineDescription) ? null : item.InlineDescription));
        }

        return candidates;
    }

    /// <summary>Usings du source genere : ce qui en vient s'ecrit sans qualification.</summary>
    private static async Task<HashSet<string>> ImportedNamespacesAsync(Document document, CancellationToken cancellationToken)
        => await document.GetSyntaxRootAsync(cancellationToken) is CompilationUnitSyntax root
            ? root.Usings.Select(u => u.Name?.ToString()).OfType<string>().ToHashSet(StringComparer.Ordinal)
            : [];

    /// <summary>
    /// Type ou methode d'un namespace absent des usings (Roslyn le met dans InlineDescription) : un type est insere
    /// avec son nom complet, la requete ne pouvant pas declarer de using ; une methode d'extension ne se qualifie
    /// pas apres un point, elle ne compilerait pas : elle est ecartee.
    /// </summary>
    private static string? InsertTextOf(RoslynCompletion.CompletionItem item, string kind, HashSet<string> imported)
    {
        var ns = item.InlineDescription;
        if (string.IsNullOrEmpty(ns) || imported.Contains(ns))
            return item.DisplayText;

        return kind switch
        {
            WellKnownTags.Method or WellKnownTags.ExtensionMethod => null,
            WellKnownTags.Class or WellKnownTags.Structure or WellKnownTags.Interface or WellKnownTags.Enum or WellKnownTags.Delegate => $"{ns}.{item.DisplayText}",
            _ => item.DisplayText,
        };
    }

    private static string KindOf(ImmutableArray<string> tags)
        => KindTags.FirstOrDefault(tags.Contains) ?? (tags.IsDefaultOrEmpty ? "Text" : tags[0]);
}
