using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;

namespace Querify.Languages.Linq.Analysis;

/// <summary>Erreurs de compilation d'un onglet LINQ, telles que l'execution les leverait.</summary>
[AutoRegister(As = typeof(IDiagnosticProvider))]
public sealed class LinqDiagnosticProvider : IDiagnosticProvider
{
    private readonly ILinqModelCache _models;
    private readonly ILinqAnalysisEngine _engine;

    public LinqDiagnosticProvider(ILinqModelCache models, ILinqAnalysisEngine engine)
    {
        _models = models;
        _engine = engine;
    }

    public async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return [];

        var model = await LinqRequestResolver.TryGetModelAsync(_models, request.Connection, request.Database, cancellationToken);
        return await _engine.GetDiagnosticsAsync(model, request.Text, LinqRequestResolver.ToParameters(request.Parameters), cancellationToken);
    }
}
