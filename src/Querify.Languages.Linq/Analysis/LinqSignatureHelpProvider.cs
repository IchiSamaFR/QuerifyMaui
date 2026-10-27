using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;

namespace Querify.Languages.Linq.Analysis;

/// <summary>Surcharges de la methode appelee (Where, Select, Contains...) pendant la saisie de ses arguments.</summary>
[AutoRegister(As = typeof(ISignatureHelpProvider))]
public sealed class LinqSignatureHelpProvider : ISignatureHelpProvider
{
    private readonly ILinqEditorServices _services;

    public LinqSignatureHelpProvider(ILinqEditorServices services)
    {
        _services = services;
    }

    public async Task<SignatureHelpInfo?> GetSignatureHelpAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return null;

        var service = await _services.GetAsync(request.Connection, request.Database, request.Parameters, cancellationToken);
        return await service.GetSignatureHelpAsync(request.Text, request.Offset, cancellationToken);
    }
}
