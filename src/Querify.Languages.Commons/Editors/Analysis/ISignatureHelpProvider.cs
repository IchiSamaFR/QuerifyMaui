using Querify.Core.Models.Editors.Analysis;
using Quill.Code.Analysis;

namespace Querify.Languages.Commons.Editors.Analysis;

/// <summary>Signature de la methode appelee au curseur ; chaque fournisseur ignore les langages qui ne sont pas les siens.</summary>
public interface ISignatureHelpProvider
{
    /// <returns>Null hors d'une liste d'arguments.</returns>
    Task<SignatureHelpInfo?> GetSignatureHelpAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default);
}
