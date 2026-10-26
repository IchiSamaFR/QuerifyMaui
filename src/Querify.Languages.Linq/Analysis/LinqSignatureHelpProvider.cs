using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;

namespace Querify.Languages.Linq.Analysis;

/// <summary>Surcharges de la methode appelee (Where, Select, Contains...) pendant la saisie de ses arguments.</summary>
[AutoRegister(As = typeof(ISignatureHelpProvider))]
public sealed class LinqSignatureHelpProvider : ISignatureHelpProvider
{
    private readonly ILinqModelCache _models;
    private readonly ILinqAnalysisEngine _engine;

    public LinqSignatureHelpProvider(ILinqModelCache models, ILinqAnalysisEngine engine)
    {
        _models = models;
        _engine = engine;
    }

    public async Task<SignatureHelpInfo?> GetSignatureHelpAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return null;

        var model = await LinqRequestResolver.TryGetModelAsync(_models, request.Connection, request.Database, cancellationToken);
        return await _engine.GetSignatureHelpAsync(model, request.Text, request.Offset, LinqRequestResolver.ToParameters(request.Parameters), cancellationToken);
    }
}
