using SqlToolManagerMaui.Core.Enums.Editors.Completion;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Editors.Completion;
using SqlToolManagerMaui.Services.Linq;
using System.Data.Common;

namespace SqlToolManagerMaui.Services.Editors.Completion;

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

    public async Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Linq)
            return [];

        LinqModelAssembly? model = null;
        if (request.Connection is { IsConnected: true } connection)
        {
            try
            {
                model = await _models.GetModelAsync(connection, cancellationToken);
            }
            catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException or LinqCompilationException)
            {
                // Base injoignable ou modele invalide : on garde la completion C# sans le contexte.
            }
        }

        var candidates = await _engine.GetCompletionsAsync(model, request.Text, request.CaretOffset, cancellationToken);

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
