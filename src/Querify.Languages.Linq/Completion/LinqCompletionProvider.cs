using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Completion;
using Querify.Databases.Models.Connections;
using Querify.Languages.Commons.Editors.Completion;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Completion;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Completion;

/// <summary>
/// IntelliSense C# des onglets LINQ (Quill) : Roslyn analyse la requete dans le contexte genere
/// (RaveOnDb. -> tables, u. -> colonnes, .Wh -> Where), mots-cles C# compris.
/// </summary>
[AutoRegister(As = typeof(ICompletionProvider))]
public sealed class LinqCompletionProvider : ICompletionProvider
{
    private readonly ILinqEditorDocuments _documents;
    private readonly ICSharpLanguageService _language;

    public LinqCompletionProvider(ILinqEditorDocuments documents, ICSharpLanguageService language)
    {
        _documents = documents;
        _language = language;
    }

    /// <summary>
    /// Composition MEF de Roslyn (1 a 2 s), JIT de la completion et compilation du DbContext genere :
    /// faits a l'ouverture de l'onglet plutot qu'a la premiere frappe.
    /// </summary>
    public async Task WarmUpAsync(SyntaxLanguage language, DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        if (language != SyntaxLanguage.Linq)
            return;

        var document = await _documents.GetAsync(string.Empty, connection, database: null, [], cancellationToken);
        await _language.WarmUpAsync(document, cancellationToken);
    }

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return [];

        var document = await _documents.GetAsync(request.Text, request.Connection, request.Database, request.Parameters, cancellationToken);
        return (await _language.GetCompletionsAsync(document, request.CaretOffset, cancellationToken)).Items;
    }
}
