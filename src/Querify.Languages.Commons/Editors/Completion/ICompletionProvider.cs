using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Editors.Completion;
using Querify.Databases.Models.Connections;
using Quill.Code.Completion;

namespace Querify.Languages.Commons.Editors.Completion;

/// <summary>
/// Source de suggestions (mots-cles, schema...). Le fournisseur renvoie tous ses candidats
/// pour le contexte : le filtrage sur le prefixe est commun et fait par <see cref="Core.Services.Editors.Completion.ICompletionService"/>.
/// </summary>
public interface ICompletionProvider
{
    Task<IReadOnlyList<CompletionItem>> GetItemsAsync(CompletionRequest request, CompletionContext context, CancellationToken cancellationToken = default);

    /// <summary>Precharge ce dont la premiere demande aura besoin (a l'ouverture d'un onglet, au changement de connexion).</summary>
    Task WarmUpAsync(SyntaxLanguage language, DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
