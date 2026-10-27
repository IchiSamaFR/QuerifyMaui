using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp.Workspaces;

namespace Querify.Languages.Linq.Workspaces;

/// <summary>
/// Projets Roslyn des requetes LINQ dans le workspace partage de Quill : un par modele,
/// references et contexte genere ne changent pas d'une frappe a l'autre.
/// </summary>
public interface ILinqWorkspace
{
    /// <summary>Document Roslyn de la requete placee dans son contexte genere (DbContext, parametres).</summary>
    /// <param name="model">DbContext compile de la connexion, ou null : C# seul.</param>
    CSharpDocument GetDocument(LinqModelAssembly? model, string query, IReadOnlyList<LinqParameter>? parameters = null);
}
