using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;

namespace Querify.Services.Editors.Analysis;

/// <inheritdoc cref="ICodeAnalysisService"/>
/// <remarks>
/// Chaque fournisseur tourne hors du thread UI : Roslyn ou le parseur T-SQL ont une partie synchrone
/// non negligeable avant leur premier await, qui figerait la souris ou la frappe.
/// </remarks>
[AutoRegister(As = typeof(ICodeAnalysisService))]
public sealed class CodeAnalysisService : ICodeAnalysisService
{
    private readonly IReadOnlyList<IHoverProvider> _hovers;
    private readonly IReadOnlyList<IDiagnosticProvider> _diagnostics;
    private readonly IReadOnlyList<ISignatureHelpProvider> _signatures;

    public CodeAnalysisService(IEnumerable<IHoverProvider> hovers, IEnumerable<IDiagnosticProvider> diagnostics, IEnumerable<ISignatureHelpProvider> signatures)
    {
        ArgumentNullException.ThrowIfNull(hovers);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(signatures);

        _hovers = hovers.ToArray();
        _diagnostics = diagnostics.ToArray();
        _signatures = signatures.ToArray();
    }

    public async Task<HoverInfo?> GetHoverAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var results = await Task.WhenAll(_hovers.Select(provider => Task.Run(() => provider.GetHoverAsync(request, cancellationToken), cancellationToken)));
        return results.FirstOrDefault(result => result is not null);
    }

    public async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var results = await Task.WhenAll(_diagnostics.Select(provider => Task.Run(() => provider.GetDiagnosticsAsync(request, cancellationToken), cancellationToken)));
        return results.SelectMany(batch => batch).OrderBy(d => d.Start).ThenBy(d => d.Length).ToArray();
    }

    public async Task<SignatureHelpInfo?> GetSignatureHelpAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var results = await Task.WhenAll(_signatures.Select(provider => Task.Run(() => provider.GetSignatureHelpAsync(request, cancellationToken), cancellationToken)));
        return results.FirstOrDefault(result => result is not null);
    }
}
