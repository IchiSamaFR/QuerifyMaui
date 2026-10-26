using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.QuickInfo;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;

namespace Querify.Languages.Linq.Analysis;

/// <inheritdoc cref="ILinqAnalysisEngine"/>
[AutoRegister(As = typeof(ILinqAnalysisEngine))]
public sealed class LinqAnalysisEngine : ILinqAnalysisEngine
{
    private readonly ILinqWorkspace _workspace;

    public LinqAnalysisEngine(ILinqWorkspace workspace)
    {
        _workspace = workspace;
    }

    // ---------- Survol ----------

    public async Task<HoverInfo?> GetHoverAsync(LinqModelAssembly? model, string query, int offset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (offset < 0 || offset > query.Length)
            return null;

        var linq = _workspace.GetDocument(model, query, parameters);
        if (QuickInfoService.GetService(linq.Document) is not { } service)
            return null;

        // Infobulle de Visual Studio : libelles localises, surcharges ; le nom touche par la souris compte aussi.
        var info = await service.GetQuickInfoAsync(linq.Document, linq.QueryStart + offset, cancellationToken);
        var signature = info?.Sections.FirstOrDefault(section => section.Kind == QuickInfoSectionKinds.Description);
        if (info is null || signature is null || !linq.Contains(info.Span))
            return null;

        // Le [Description] du modele genere (table, colonne SQL) prime sur la documentation XML.
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(linq.Document, info.Span.Start, cancellationToken);
        var details = (symbol is null ? null : LinqSymbolFormatter.Details(symbol))
            ?? info.Sections.FirstOrDefault(section => section.Kind == QuickInfoSectionKinds.DocumentationComments)?.Text;

        return new HoverInfo(info.Span.Start - linq.QueryStart, info.Span.Length, signature.Text, details);
    }

    // ---------- Erreurs ----------

    public async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(LinqModelAssembly? model, string query, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query))
            return [];

        var linq = _workspace.GetDocument(model, query, parameters);
        if (await linq.Document.GetSyntaxTreeAsync(cancellationToken) is not { } tree)
            return [];

        var syntax = tree.GetDiagnostics(cancellationToken).ToHashSet();
        IEnumerable<Diagnostic> all = syntax;

        // Sans DbContext, la variable de la base n'existe pas : seules les fautes de syntaxe ont un sens.
        if (linq.HasModel && await linq.Document.GetSemanticModelAsync(cancellationToken) is { } semantic)
            all = semantic.GetDiagnostics(cancellationToken: cancellationToken);

        return LinqDiagnosticMapper.ToQuery(all, syntax, linq, query);
    }

    // ---------- Signatures ----------

    public async Task<SignatureHelpInfo?> GetSignatureHelpAsync(LinqModelAssembly? model, string query, int offset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (offset <= 0 || offset > query.Length)
            return null;

        var linq = _workspace.GetDocument(model, query, parameters);
        if (await linq.Document.GetSyntaxRootAsync(cancellationToken) is not { } root)
            return null;

        var position = linq.QueryStart + offset;
        var argumentList = FindArgumentList(root, position);
        if (argumentList?.Parent is not InvocationExpressionSyntax invocation || !linq.Contains(argumentList.OpenParenToken.Span))
            return null;

        if (await linq.Document.GetSemanticModelAsync(cancellationToken) is not { } semantic)
            return null;

        // Toutes les surcharges visibles (methodes d'extension comprises) ; a defaut, les candidates d'un appel non resolu.
        var resolved = semantic.GetSymbolInfo(invocation, cancellationToken);
        var methods = semantic.GetMemberGroup(invocation.Expression, cancellationToken).OfType<IMethodSymbol>().ToList();
        if (methods.Count == 0)
            methods = resolved.CandidateSymbols.OfType<IMethodSymbol>().ToList();

        if (methods.Count == 0)
            return null;

        var activeParameter = argumentList.Arguments.GetSeparators().Count(separator => separator.SpanStart < position);
        var activeSignature = resolved.Symbol is IMethodSymbol chosen ? methods.FindIndex(m => IsSameMethod(m, chosen)) : -1;
        if (activeSignature < 0)
            activeSignature = Math.Max(0, methods.FindIndex(m => m.Parameters.Length > activeParameter || m.Parameters.LastOrDefault()?.IsParams == true));

        return new SignatureHelpInfo(
            methods.Select(LinqSymbolFormatter.ToSignature).ToArray(),
            activeSignature,
            activeParameter,
            argumentList.OpenParenToken.SpanStart - linq.QueryStart);
    }

    /// <summary>Meme methode, qu'elle soit vue comme extension reduite (x.Where) ou statique (Queryable.Where).</summary>
    private static bool IsSameMethod(IMethodSymbol left, IMethodSymbol right)
        => SymbolEqualityComparer.Default.Equals((left.ReducedFrom ?? left).OriginalDefinition, (right.ReducedFrom ?? right).OriginalDefinition);

    /// <summary>Liste d'arguments la plus interieure dont le curseur est entre les parentheses.</summary>
    private static ArgumentListSyntax? FindArgumentList(SyntaxNode root, int position)
    {
        var token = root.FindToken(position - 1);

        return token.Parent?
            .AncestorsAndSelf()
            .OfType<ArgumentListSyntax>()
            .FirstOrDefault(list => list.OpenParenToken.Span.End <= position
                && (list.CloseParenToken.IsMissing || position <= list.CloseParenToken.SpanStart));
    }
}
