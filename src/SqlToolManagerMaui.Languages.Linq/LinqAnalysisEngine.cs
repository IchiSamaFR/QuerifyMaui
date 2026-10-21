using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using SqlToolManagerMaui.Core.Enums.Editors.Analysis;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors.Analysis;
using System.Globalization;

namespace SqlToolManagerMaui.Languages.Linq;

/// <inheritdoc cref="ILinqAnalysisEngine"/>
[AutoRegister(As = typeof(ILinqAnalysisEngine))]
public sealed class LinqAnalysisEngine : ILinqAnalysisEngine
{
    /// <summary>Au-dela, une erreur en amont fait cascader les suivantes : les premieres suffisent.</summary>
    private const int MaxDiagnostics = 100;

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
        if (await linq.Document.GetSyntaxRootAsync(cancellationToken) is not { } root)
            return null;

        // Le caractere survole, ou celui qui le precede (souris posee juste apres la fin du nom).
        var position = linq.QueryStart + offset;
        var name = FindName(root, position) ?? (offset > 0 ? FindName(root, position - 1) : null);
        if (name is not { } token || !linq.Contains(token.Span))
            return null;

        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(linq.Document, token.SpanStart, cancellationToken);
        if (symbol is null)
            return null;

        return new HoverInfo(token.SpanStart - linq.QueryStart, token.Span.Length, LinqSymbolFormatter.Describe(symbol), LinqSymbolFormatter.Details(symbol));
    }

    /// <summary>Identifiant (var compris) ou type predefini (int, string) a cette position.</summary>
    private static SyntaxToken? FindName(SyntaxNode root, int position)
    {
        var token = root.FindToken(position);
        if (!token.Span.Contains(position))
            return null;

        return token.IsKind(SyntaxKind.IdentifierToken) || SyntaxFacts.IsPredefinedType(token.Kind()) ? token : null;
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

        List<CodeDiagnostic> diagnostics = [];
        foreach (var diagnostic in all.OrderBy(d => d.Location.SourceSpan.Start))
        {
            if (diagnostic.Severity is not (DiagnosticSeverity.Error or DiagnosticSeverity.Warning) || diagnostic.IsSuppressed || !diagnostic.Location.IsInSource)
                continue;

            var span = diagnostic.Location.SourceSpan;
            var start = span.Start - linq.QueryStart;
            if (start < 0)
                continue;

            if (start > query.Length)
            {
                // ")" ou ";" attendu est signale sur le code genere qui suit la requete : on le rattache a sa fin.
                // Une erreur semantique au-dela de la requete concerne le code genere lui-meme.
                if (!syntax.Contains(diagnostic))
                    continue;

                start = query.Length;
            }

            var (visibleStart, visibleLength) = Widen(query, start, Math.Min(span.Length, query.Length - start));
            if (visibleLength == 0)
                continue;

            diagnostics.Add(new CodeDiagnostic(
                visibleStart,
                visibleLength,
                diagnostic.Severity == DiagnosticSeverity.Error ? CodeDiagnosticSeverity.Error : CodeDiagnosticSeverity.Warning,
                diagnostic.GetMessage(CultureInfo.CurrentUICulture),
                diagnostic.Id));

            if (diagnostics.Count == MaxDiagnostics)
                break;
        }

        return diagnostics;
    }

    /// <summary>Une erreur sans longueur (symbole manquant) souligne le caractere visible le plus proche.</summary>
    private static (int Start, int Length) Widen(string query, int start, int length)
    {
        if (length > 0)
            return (start, length);

        if (start < query.Length && !char.IsWhiteSpace(query[start]))
            return (start, 1);

        var previous = start - 1;
        while (previous >= 0 && char.IsWhiteSpace(query[previous]))
            previous--;

        return previous >= 0 ? (previous, 1) : (start, 0);
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
