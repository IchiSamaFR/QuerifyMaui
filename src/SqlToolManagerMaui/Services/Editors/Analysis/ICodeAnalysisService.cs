using SqlToolManagerMaui.Core.Models.Editors.Analysis;

namespace SqlToolManagerMaui.Services.Editors.Analysis;

/// <summary>Point d'entree de l'editeur pour le survol, les signatures et les erreurs, tous langages confondus.</summary>
public interface ICodeAnalysisService
{
    /// <summary>Premiere infobulle trouvee par les fournisseurs ; null si la position ne designe rien.</summary>
    Task<HoverInfo?> GetHoverAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default);

    /// <summary>Erreurs et avertissements de tous les fournisseurs, dans l'ordre du texte.</summary>
    Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default);

    /// <summary>Premiere aide a la signature trouvee ; null hors d'une liste d'arguments.</summary>
    Task<SignatureHelpInfo?> GetSignatureHelpAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default);
}
