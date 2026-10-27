using Querify.Core.Models.Editors.Analysis;
using Quill.Code.Analysis;

namespace Querify.Languages.Commons.Editors.Analysis;

/// <summary>Erreurs et avertissements d'un texte ; chaque fournisseur ignore les langages qui ne sont pas les siens.</summary>
public interface IDiagnosticProvider
{
    Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default);
}
