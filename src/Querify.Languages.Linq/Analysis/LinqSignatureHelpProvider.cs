using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Analysis;

/// <summary>Surcharges de la methode appelee (Where, Select, Contains...) pendant la saisie de ses arguments.</summary>
[AutoRegister(As = typeof(ISignatureHelpProvider))]
public sealed class LinqSignatureHelpProvider : ISignatureHelpProvider
{
    private readonly ILinqEditorDocuments _documents;
    private readonly ICSharpLanguageService _language;

    public LinqSignatureHelpProvider(ILinqEditorDocuments documents, ICSharpLanguageService language)
    {
        _documents = documents;
        _language = language;
    }

    public async Task<SignatureHelpInfo?> GetSignatureHelpAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return null;

        var document = await _documents.GetAsync(request.Text, request.Connection, request.Database, request.Parameters, cancellationToken);
        return await _language.GetSignatureHelpAsync(document, request.Offset, cancellationToken);
    }
}
