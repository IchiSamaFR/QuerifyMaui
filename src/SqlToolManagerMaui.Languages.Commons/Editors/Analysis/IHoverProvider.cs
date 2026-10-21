using SqlToolManagerMaui.Core.Models.Editors.Analysis;

namespace SqlToolManagerMaui.Languages.Commons.Editors.Analysis;

/// <summary>Infobulle du nom survole ; chaque fournisseur ignore les langages qui ne sont pas les siens.</summary>
public interface IHoverProvider
{
    /// <returns>Null si la position ne designe rien de connu.</returns>
    Task<HoverInfo?> GetHoverAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default);
}
