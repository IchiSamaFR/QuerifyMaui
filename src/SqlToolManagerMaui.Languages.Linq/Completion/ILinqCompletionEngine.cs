using SqlToolManagerMaui.Languages.Linq.Compilation;
using SqlToolManagerMaui.Languages.Linq.Queries;

namespace SqlToolManagerMaui.Languages.Linq.Completion;

/// <summary>IntelliSense C# de Roslyn sur le texte d'une requete LINQ, contexte genere compris.</summary>
public interface ILinqCompletionEngine
{
    /// <param name="model">DbContext compile de la connexion, ou null : completion C# seule.</param>
    /// <param name="parameters">Variables declarees avant la requete (parametres du dashboard).</param>
    Task<IReadOnlyList<LinqCompletionCandidate>> GetCompletionsAsync(LinqModelAssembly? model, string query, int caretOffset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default);
}
