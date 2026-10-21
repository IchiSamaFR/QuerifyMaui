using SqlToolManagerMaui.Core.Enums.Editors.Completion;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors.Completion;
using SqlToolManagerMaui.Languages.Commons.Editors.Completion;

namespace SqlToolManagerMaui.Services.Editors.Completion;

/// <summary>Parametres fournis a l'execution (dashboard) : proposes en @Nom dans une requete SQL sans DECLARE.</summary>
[AutoRegister(As = typeof(ICompletionProvider))]
public sealed class QueryParameterCompletionProvider : ICompletionProvider
{
    public Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        // Une valeur s'ecrit dans une expression, pas apres un point ni a la place d'une table.
        if (request.Language != SyntaxLanguage.Sql || request.Parameters.Count == 0
            || context.Kind is not (CompletionContextKind.General or CompletionContextKind.Column))
            return Task.FromResult<IReadOnlyList<CompletionItem>>([]);

        IReadOnlyList<CompletionItem> items = request.Parameters
            .Select(p => new CompletionItem
            {
                Label = "@" + p.Name,
                InsertText = "@" + p.Name,
                Kind = CompletionItemKind.Variable,
                Detail = p.Type.ToString(),
            })
            .ToArray();

        return Task.FromResult(items);
    }
}
