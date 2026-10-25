using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors.Analysis;
using SqlToolManagerMaui.Languages.Commons.Editors.Analysis;
using SqlToolManagerMaui.Languages.Linq.Modeling;
using SqlToolManagerMaui.Languages.Linq.Queries;

namespace SqlToolManagerMaui.Languages.Linq.Analysis;

/// <summary>Type et origine (table, colonne) du nom survole dans un onglet LINQ.</summary>
[AutoRegister(As = typeof(IHoverProvider))]
public sealed class LinqHoverProvider : IHoverProvider
{
    private readonly ILinqModelCache _models;
    private readonly ILinqAnalysisEngine _engine;

    public LinqHoverProvider(ILinqModelCache models, ILinqAnalysisEngine engine)
    {
        _models = models;
        _engine = engine;
    }

    public async Task<HoverInfo?> GetHoverAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return null;

        var model = await LinqRequestResolver.TryGetModelAsync(_models, request.Connection, request.Database, cancellationToken);
        return await _engine.GetHoverAsync(model, request.Text, request.Offset, LinqRequestResolver.ToParameters(request.Parameters), cancellationToken);
    }
}
