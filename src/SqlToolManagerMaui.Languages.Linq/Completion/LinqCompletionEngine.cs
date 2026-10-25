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
        var root = await document.GetSyntaxRootAsync(cancellationToken);

        // Roslyn propose tout ce qui est visible (System.Security, System.IO, snippets...) :
        // on ne garde que ce qui sert a ecrire une requete EF.
        var filter = new LinqCompletionFilter(model?.Model, root is null ? null : MemberAccessTarget(root, list.Span.Start));

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
    /// chaine vide si c'est autre chose (appel, indexeur, u?.) ; null hors acces membre.
    /// </summary>
    private static string? MemberAccessTarget(SyntaxNode root, int wordStart)
    {
        // Mot vide (curseur juste apres le point) : FindToken rend le jeton suivant, dont le precedent est le point.
        var dot = root.FindToken(wordStart).GetPreviousToken();
        if (!dot.IsKind(SyntaxKind.DotToken) || dot.Span.End > wordStart)
            return null;

        return dot.Parent switch
        {
            MemberAccessExpressionSyntax access => LastName(access.Expression),
            QualifiedNameSyntax qualified => LastName(qualified.Left),
            _ => string.Empty,
        };
    }

    private static string LastName(SyntaxNode node) => node switch
    {
        SimpleNameSyntax name => name.Identifier.ValueText,
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        _ => string.Empty,
    };

    private static string KindOf(ImmutableArray<string> tags)
        => KindTags.FirstOrDefault(tags.Contains) ?? (tags.IsDefaultOrEmpty ? "Text" : tags[0]);
}
