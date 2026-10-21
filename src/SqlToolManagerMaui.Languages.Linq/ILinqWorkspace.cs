namespace SqlToolManagerMaui.Languages.Linq;

/// <summary>
/// Workspace Roslyn partage par l'autocompletion, le survol, les signatures et les erreurs :
/// la composition MEF et les references ne sont chargees qu'une fois pour l'application.
/// </summary>
public interface ILinqWorkspace
{
    /// <summary>Document Roslyn de la requete placee dans son contexte genere (DbContext, parametres).</summary>
    /// <param name="model">DbContext compile de la connexion, ou null : C# seul.</param>
    LinqDocument GetDocument(LinqModelAssembly? model, string query, IReadOnlyList<LinqParameter>? parameters = null);
}
