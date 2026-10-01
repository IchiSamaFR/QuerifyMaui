using SqlToolManagerMaui.Models.Editors.Completion;

namespace SqlToolManagerMaui.Services.Editors.Completion;

/// <summary>
/// Source de suggestions (mots-cles, schema...). Le fournisseur renvoie tous ses candidats
/// pour le contexte : le filtrage sur le prefixe est commun et fait par <see cref="ICompletionService"/>.
/// </summary>
public interface ICompletionProvider
{
    Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default);
}
