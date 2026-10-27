using Querify.Core.Ioc;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Analysis;

/// <inheritdoc cref="ILinqAnalysisEngine"/>
[AutoRegister(As = typeof(ILinqAnalysisEngine))]
public sealed class LinqAnalysisEngine : ILinqAnalysisEngine
{
    private readonly ILinqWorkspace _workspace;
    private readonly ICSharpLanguageService _language;

    public LinqAnalysisEngine(ILinqWorkspace workspace, ICSharpLanguageService language)
    {
        _workspace = workspace;
        _language = language;
    }

    public Task<HoverInfo?> GetHoverAsync(LinqModelAssembly? model, string query, int offset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _language.GetHoverAsync(_workspace.GetDocument(model, query, parameters), offset, cancellationToken);
    }

    public async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(LinqModelAssembly? model, string query, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var diagnostics = await _language.GetDiagnosticsAsync(_workspace.GetDocument(model, query, parameters), cancellationToken);

        // Les informations (suggestions) ne sont pas soulignees dans un onglet LINQ.
        return diagnostics.Where(d => d.Severity != CodeDiagnosticSeverity.Info).ToArray();
    }

    public Task<SignatureHelpInfo?> GetSignatureHelpAsync(LinqModelAssembly? model, string query, int offset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _language.GetSignatureHelpAsync(_workspace.GetDocument(model, query, parameters), offset, cancellationToken);
    }
}
