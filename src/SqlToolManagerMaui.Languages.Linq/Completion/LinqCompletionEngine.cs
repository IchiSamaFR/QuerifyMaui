using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Tags;
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

        // Mot vide (curseur juste apres le point) : FindToken rend le jeton suivant, dont le precedent est le point.
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        var dot = root?.FindToken(list.Span.Start).GetPreviousToken() ?? default;
        var isMemberAccess = dot.IsKind(SyntaxKind.DotToken);
        var contextMembers = dot.Parent is MemberAccessExpressionSyntax access
            ? await ContextMembersAsync(document, access.Expression, cancellationToken)
            : null;

        // Roslyn propose tout ce qui est visible (System.Security, System.IO, snippets...) :
        // on ne garde que ce qui sert a ecrire une requete EF.
        var filter = new LinqCompletionFilter(model?.Model, isMemberAccess, contextMembers);

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
    /// Proprietes declarees par le DbContext genere (ses DbSet) si l'expression avant le point en est un ;
    /// null sinon. Le reste (SaveChanges, ChangeTracker, Database...) vient de DbContext : plomberie EF.
    /// </summary>
    private static async Task<IReadOnlySet<string>?> ContextMembersAsync(Document document, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        if (await document.GetSemanticModelAsync(cancellationToken) is not { } semantic
            || semantic.Compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext") is not { } dbContext
            || semantic.GetTypeInfo(expression, cancellationToken).Type is not INamedTypeSymbol type)
            return null;

        for (var parent = type.BaseType; parent is not null; parent = parent.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(parent, dbContext))
                return type.GetMembers().OfType<IPropertySymbol>().Where(p => p.DeclaredAccessibility == Accessibility.Public).Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        }

        return null;
    }

    private static string KindOf(ImmutableArray<string> tags)
        => KindTags.FirstOrDefault(tags.Contains) ?? (tags.IsDefaultOrEmpty ? "Text" : tags[0]);
}
