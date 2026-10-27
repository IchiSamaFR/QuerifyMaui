using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Quill.Code.Completion;

namespace Querify.Languages.Linq.Completion;

/// <summary>IntelliSense C# de Roslyn (Quill) sur le texte d'une requete LINQ, contexte genere compris.</summary>
public interface ILinqCompletionEngine
{
    /// <param name="model">DbContext compile de la connexion, ou null : completion C# seule.</param>
    /// <param name="parameters">Variables declarees avant la requete (parametres du dashboard).</param>
    Task<CompletionList> GetCompletionsAsync(LinqModelAssembly? model, string query, int caretOffset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default);
}
