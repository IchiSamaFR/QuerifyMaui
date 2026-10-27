using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Analysis;

/// <summary>Erreurs de compilation d'un onglet LINQ, telles que l'execution les leverait ; sans DbContext, seules les fautes de syntaxe.</summary>
[AutoRegister(As = typeof(IDiagnosticProvider))]
public sealed class LinqDiagnosticProvider : IDiagnosticProvider
{
    private readonly ILinqEditorDocuments _documents;
    private readonly ICSharpLanguageService _language;

    public LinqDiagnosticProvider(ILinqEditorDocuments documents, ICSharpLanguageService language)
    {
        _documents = documents;
        _language = language;
    }

    public async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return [];

        var document = await _documents.GetAsync(request.Text, request.Connection, request.Database, request.Parameters, cancellationToken);
        var diagnostics = await _language.GetDiagnosticsAsync(document, cancellationToken);

        // Les informations (suggestions) ne sont pas soulignees dans un onglet LINQ.
        return diagnostics.Where(d => d.Severity != CodeDiagnosticSeverity.Info).ToArray();
    }
}
