using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Analysis;

/// <summary>Type et origine (table, colonne) du nom survole dans un onglet LINQ.</summary>
[AutoRegister(As = typeof(IHoverProvider))]
public sealed class LinqHoverProvider : IHoverProvider
{
    private readonly ILinqEditorDocuments _documents;
    private readonly ICSharpLanguageService _language;

    public LinqHoverProvider(ILinqEditorDocuments documents, ICSharpLanguageService language)
    {
        _documents = documents;
        _language = language;
    }

    public async Task<HoverInfo?> GetHoverAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return null;

        var document = await _documents.GetAsync(request.Text, request.Connection, request.Database, request.Parameters, cancellationToken);
        return await _language.GetHoverAsync(document, request.Offset, cancellationToken);
    }
}
