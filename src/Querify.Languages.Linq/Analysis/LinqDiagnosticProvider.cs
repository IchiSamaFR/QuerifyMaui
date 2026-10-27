using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;

namespace Querify.Languages.Linq.Analysis;

/// <summary>Erreurs de compilation d'un onglet LINQ, telles que l'execution les leverait ; sans DbContext, seules les fautes de syntaxe.</summary>
[AutoRegister(As = typeof(IDiagnosticProvider))]
public sealed class LinqDiagnosticProvider : IDiagnosticProvider
{
    private readonly ILinqEditorServices _services;

    public LinqDiagnosticProvider(ILinqEditorServices services)
    {
        _services = services;
    }

    public async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return [];

        var service = await _services.GetAsync(request.Connection, request.Database, request.Parameters, cancellationToken);
        var diagnostics = await service.GetDiagnosticsAsync(request.Text, cancellationToken);

        // Les informations (suggestions) ne sont pas soulignees dans un onglet LINQ.
        return diagnostics.Where(d => d.Severity != CodeDiagnosticSeverity.Info).ToArray();
    }
}
