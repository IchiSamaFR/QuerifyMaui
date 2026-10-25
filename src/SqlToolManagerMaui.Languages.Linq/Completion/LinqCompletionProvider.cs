using SqlToolManagerMaui.Core.Enums.Editors.Completion;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors.Completion;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Languages.Commons.Editors.Completion;
using SqlToolManagerMaui.Languages.Linq.Compilation;
using SqlToolManagerMaui.Languages.Linq.Modeling;
using SqlToolManagerMaui.Languages.Linq.Queries;

namespace SqlToolManagerMaui.Languages.Linq.Completion;

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
        "Method" or "ExtensionMethod" => CompletionItemKind.Method,
        "Property" => CompletionItemKind.Property,
        "Field" or "EnumMember" or "Event" => CompletionItemKind.Field,
        "Local" or "Parameter" or "RangeVariable" => CompletionItemKind.Variable,
        "Class" or "Structure" or "Interface" or "Enum" or "Delegate" => CompletionItemKind.Type,
        "Namespace" => CompletionItemKind.Namespace,
        "Keyword" => CompletionItemKind.Keyword,
        "Snippet" => CompletionItemKind.Snippet,
        _ => CompletionItemKind.Text
    };
}
