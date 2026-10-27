using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Completion;
using Querify.Databases.Models.Connections;
using Querify.Languages.Commons.Editors.Completion;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Completion;

namespace Querify.Languages.Linq.Completion;

/// <summary>
/// IntelliSense C# des onglets LINQ (Quill) : Roslyn analyse la requete dans le contexte genere
/// (RaveOnDb. -> tables, u. -> colonnes, .Wh -> Where), mots-cles C# compris.
/// </summary>
[AutoRegister(As = typeof(ICompletionProvider))]
public sealed class LinqCompletionProvider : ICompletionProvider
{
    private readonly ILinqEditorServices _services;

    public LinqCompletionProvider(ILinqEditorServices services)
    {
        _services = services;
    }

    /// <summary>
    /// Composition MEF de Roslyn (1 a 2 s), JIT de la completion et compilation du DbContext genere :
    /// faits a l'ouverture de l'onglet plutot qu'a la premiere frappe.
    /// </summary>
    public async Task WarmUpAsync(SyntaxLanguage language, DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        if (language != SyntaxLanguage.Linq)
            return;

        var service = await _services.GetAsync(connection, database: null, [], cancellationToken);
        await service.WarmUpAsync(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return [];

        var service = await _services.GetAsync(request.Connection, request.Database, request.Parameters, cancellationToken);
        return (await service.GetCompletionsAsync(request.Text, request.CaretOffset, cancellationToken)).Items;
    }
}
