using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;

namespace Querify.Languages.Linq.Analysis;

/// <summary>Type et origine (table, colonne) du nom survole dans un onglet LINQ.</summary>
[AutoRegister(As = typeof(IHoverProvider))]
public sealed class LinqHoverProvider : IHoverProvider
{
    private readonly ILinqEditorServices _services;

    public LinqHoverProvider(ILinqEditorServices services)
    {
        _services = services;
    }

    public async Task<HoverInfo?> GetHoverAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return null;

        var service = await _services.GetAsync(request.Connection, request.Database, request.Parameters, cancellationToken);
        return await service.GetHoverAsync(request.Text, request.Offset, cancellationToken);
    }
}
