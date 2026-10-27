using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Workspaces;

/// <summary>
/// Services C# de Quill pour les requetes LINQ : un par modele et jeu de parametres, dont le projet place la requete
/// dans son contexte genere (DbContext, parametres). Partages par l'editeur et le compilateur.
/// </summary>
public interface ILinqLanguageServices
{
    /// <param name="model">DbContext compile de la connexion, ou null : C# seul (fautes de syntaxe uniquement).</param>
    /// <param name="parameters">Variables declarees avant la requete (parametres du dashboard).</param>
    ICSharpLanguageService Get(LinqModelAssembly? model, IReadOnlyList<LinqParameter>? parameters = null);
}
