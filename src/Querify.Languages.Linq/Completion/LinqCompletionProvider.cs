using Microsoft.CodeAnalysis.Tags;
using Querify.Core.Enums.Editors.Completion;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Completion;
using Querify.Databases.Models.Connections;
using Querify.Languages.Commons.Editors.Completion;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;

namespace Querify.Languages.Linq.Completion;

/// <summary>
/// IntelliSense C# des onglets LINQ : Roslyn analyse la requete dans le contexte genere
/// (RaveOnDb. -> tables, u. -> colonnes, .Wh -> Where), mots-cles C# compris.
/// </summary>
[AutoRegister(As = typeof(ICompletionProvider))]
public sealed class LinqCompletionProvider : ICompletionProvider
{
    private readonly ILinqModelCache _models;
    private readonly ILinqCompletionEngine _engine;

    public LinqCompletionProvider(ILinqModelCache models, ILinqCompletionEngine engine)
    {
        _models = models;
        _engine = engine;
    }

    /// <summary>
    /// Composition MEF de Roslyn (1 a 2 s), JIT de la completion et compilation du DbContext genere :
    /// faits a l'ouverture de l'onglet plutot qu'a la premiere frappe.
    /// </summary>
    public async Task WarmUpAsync(SyntaxLanguage language, DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        if (language != SyntaxLanguage.Linq)
            return;

        LinqModelAssembly? model = null;
        if (connection is { IsConnected: true })
            model = await _models.GetModelAsync(connection, database: null, cancellationToken);

        await _engine.GetCompletionsAsync(model, string.Empty, 0, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return [];

        var model = await LinqRequestResolver.TryGetModelAsync(_models, request.Connection, request.Database, cancellationToken);
        var parameters = LinqRequestResolver.ToParameters(request.Parameters);

        var candidates = await _engine.GetCompletionsAsync(model, request.Text, request.CaretOffset, parameters, cancellationToken);

        return candidates.Select(c => new CompletionItem
        {
            Label = c.Label,
            InsertText = c.InsertText,
            Kind = ToKind(c.Kind),
            Detail = c.Detail
        }).ToArray();
    }

    private static CompletionItemKind ToKind(string roslynKind) => roslynKind switch
    {
        WellKnownTags.Method or WellKnownTags.ExtensionMethod => CompletionItemKind.Method,
        WellKnownTags.Property => CompletionItemKind.Property,
        WellKnownTags.Field or WellKnownTags.EnumMember or WellKnownTags.Event => CompletionItemKind.Field,
        WellKnownTags.Local or WellKnownTags.Parameter or WellKnownTags.RangeVariable => CompletionItemKind.Variable,
        WellKnownTags.Class or WellKnownTags.Structure or WellKnownTags.Interface or WellKnownTags.Enum or WellKnownTags.Delegate => CompletionItemKind.Type,
        WellKnownTags.Namespace => CompletionItemKind.Namespace,
        WellKnownTags.Keyword => CompletionItemKind.Keyword,
        WellKnownTags.Snippet => CompletionItemKind.Snippet,
        _ => CompletionItemKind.Text
    };
}
