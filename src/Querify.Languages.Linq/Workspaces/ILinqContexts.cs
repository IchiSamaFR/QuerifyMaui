using Querify.Databases.Models.Connections;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Workspaces;

/// <summary>
/// Contexte LINQ d'une base : DbContext genere puis compile (une fois par catalogue charge), QueryGlobals compile (une fois
/// par modele et jeu de parametres), et service C# de Quill qui place la requete dans ce contexte. Partage par l'editeur
/// et l'execution : le premier paie la generation.
/// </summary>
public interface ILinqContexts
{
    /// <summary>Modele de <paramref name="database"/> (null = base par defaut de la connexion) ; leve en cas d'echec.</summary>
    Task<LinqGeneratedModelAssembly> GetModelAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken = default);

    /// <summary>
    /// QueryGlobals de <paramref name="model"/> (null = C# seul) et des <paramref name="parameters"/> (noms et types ; les
    /// valeurs sont posees a l'execution) ; leve <see cref="LinqCompilationException"/> en cas d'echec.
    /// </summary>
    Task<LinqParametersAssembly> GetParametersAsync(LinqGeneratedModelAssembly? model, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Service C# d'une demande de l'editeur : avec le DbContext de la base s'il est disponible, sinon C# seul
    /// (pas de connexion, base injoignable, modele invalide).
    /// </summary>
    /// <param name="database">Base interrogee ; null = base de la connexion.</param>
    /// <param name="parameters">Parametres du dashboard, que le script utilise comme variables ; null : aucun.</param>
    Task<CSharpLanguageService> GetLanguageServiceAsync(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default);
}
