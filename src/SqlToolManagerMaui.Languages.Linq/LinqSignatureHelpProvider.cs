using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors.Analysis;
using SqlToolManagerMaui.Languages.Commons.Editors.Analysis;

namespace SqlToolManagerMaui.Languages.Linq;

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
