using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Core.Models.Editors.Completion;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Querify.Languages.Commons.Editors.Analysis;
using Querify.Languages.Commons.Editors.Completion;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Editors;

/// <summary>
/// Editeur des onglets LINQ (Quill) : Roslyn analyse la requete, C# natif, contre le contexte genere (context. -> tables,
/// u. -> colonnes, .Wh -> Where). Autocompletion, erreurs (telles que l'execution les leverait ; sans DbContext,
/// seules les fautes de syntaxe), survol (type et origine du nom) et surcharges de la methode appelee.
/// </summary>
[AutoRegister(As = typeof(ICompletionProvider))]
[AutoRegister(As = typeof(IDiagnosticProvider))]
[AutoRegister(As = typeof(IHoverProvider))]
[AutoRegister(As = typeof(ISignatureHelpProvider))]
public sealed class LinqEditorProvider : ICompletionProvider, IDiagnosticProvider, IHoverProvider, ISignatureHelpProvider
{
    private readonly ILinqContexts _contexts;

    public LinqEditorProvider(ILinqContexts contexts)
    {
        _contexts = contexts;
    }

    /// <summary>
    /// Composition MEF de Roslyn (1 a 2 s), JIT de la completion et compilation du DbContext genere :
    /// faits a l'ouverture de l'onglet plutot qu'a la premiere frappe.
    /// </summary>
    public async Task WarmUpAsync(SyntaxLanguage language, DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        if (language != SyntaxLanguage.Linq)
            return;

        var service = await _contexts.GetLanguageServiceAsync(connection, database: null, cancellationToken: cancellationToken);
        await service.WarmUpAsync(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return [];

        var service = await _contexts.GetLanguageServiceAsync(request.Connection, request.Database, ToVariables(request.Parameters), cancellationToken);
        return (await service.GetCompletionsAsync(request.Text, request.CaretOffset, cancellationToken)).Items;
    }

    public async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        if (await GetLanguageServiceAsync(request, cancellationToken) is not { } service)
            return [];

        // Les informations (suggestions) ne sont pas soulignees dans un onglet LINQ.
        var diagnostics = await service.GetDiagnosticsAsync(request.Text, cancellationToken);
        return diagnostics.Where(d => d.Severity != CodeDiagnosticSeverity.Info && d.Code != LinqQuerySource.NoEntryPointCode).ToArray();
    }

    public async Task<HoverInfo?> GetHoverAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
        => await GetLanguageServiceAsync(request, cancellationToken) is { } service
            ? await service.GetHoverAsync(request.Text, request.Offset, cancellationToken)
            : null;

    public async Task<SignatureHelpInfo?> GetSignatureHelpAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
        => await GetLanguageServiceAsync(request, cancellationToken) is { } service
            ? await service.GetSignatureHelpAsync(request.Text, request.Offset, cancellationToken)
            : null;

    /// <summary>Service de la demande, ou null si elle ne concerne pas un onglet LINQ.</summary>
    private async Task<ICSharpLanguageService?> GetLanguageServiceAsync(CodeAnalysisRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Language == SyntaxLanguage.Linq
            ? await _contexts.GetLanguageServiceAsync(request.Connection, request.Database, ToVariables(request.Parameters), cancellationToken)
            : null;
    }

    /// <summary>Parametres du dashboard vus du script : memes noms et types qu'a l'execution, sans les valeurs (le service de l'editeur reste stable).</summary>
    private static LinqParameter[] ToVariables(IReadOnlyList<QueryParameter> parameters) => [.. parameters.Select(p => LinqParameter.From(p) with { Value = null })];
}
