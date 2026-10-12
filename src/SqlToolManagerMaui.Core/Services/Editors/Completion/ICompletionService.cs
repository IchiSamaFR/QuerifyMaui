using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Editors.Completion;

namespace SqlToolManagerMaui.Core.Services.Editors.Completion;

/// <summary>Point d'entree de l'autocompletion pour l'editeur.</summary>
public interface ICompletionService
{
    /// <summary>Candidats de tous les fournisseurs, charges une fois a l'ouverture de la liste.</summary>
    Task<IReadOnlyList<CompletionItem>> GetCandidatesAsync(CompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Filtre et trie les candidats sur le prefixe frappe ; rejoue a chaque touche, sans I/O.</summary>
    IReadOnlyList<CompletionItem> Filter(IReadOnlyList<CompletionItem> candidates, string prefix);

    /// <summary>Precharge les fournisseurs (catalogue, Roslyn...) en arriere-plan ; n'echoue jamais.</summary>
    Task WarmUpAsync(SyntaxLanguage language, DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default);
}
