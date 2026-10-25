using SqlToolManagerMaui.Core.Models.Editors.Analysis;
using SqlToolManagerMaui.Languages.Linq.Compilation;
using SqlToolManagerMaui.Languages.Linq.Queries;

namespace SqlToolManagerMaui.Languages.Linq.Analysis;

/// <summary>Survol, signatures et erreurs d'une requete LINQ, calcules par Roslyn dans le contexte genere.</summary>
/// <remarks>Les positions recues et rendues sont celles du texte de la requete, pas du source genere.</remarks>
public interface ILinqAnalysisEngine
{
    /// <param name="model">DbContext compile de la connexion, ou null : C# seul.</param>
    Task<HoverInfo?> GetHoverAsync(LinqModelAssembly? model, string query, int offset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>Sans modele, seules les fautes de syntaxe : la variable de la base n'existerait pas.</summary>
    Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(LinqModelAssembly? model, string query, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default);

    Task<SignatureHelpInfo?> GetSignatureHelpAsync(LinqModelAssembly? model, string query, int offset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default);
}
