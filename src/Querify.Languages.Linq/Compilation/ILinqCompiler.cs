using Microsoft.EntityFrameworkCore;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;

namespace Querify.Languages.Linq.Compilation;

/// <summary>
/// Compilation Roslyn en memoire : le DbContext genere (une fois par schema), puis chaque
/// requete LINQ, enveloppee dans une methode qui recoit le contexte sous le nom de la base.
/// </summary>
public interface ILinqCompiler
{
    /// <summary>Compile et charge le modele genere ; leve <see cref="LinqCompilationException"/> en cas d'echec.</summary>
    LinqModelAssembly CompileModel(EfGeneratedModel model);

    /// <summary>
    /// Compile une expression LINQ depuis le document du workspace (celui que l'editeur analyse) ; les erreurs
    /// sont rapportees, pas levees. Les <paramref name="parameters"/> sont declares en variables avant la requete,
    /// leurs valeurs fixees pour l'execution.
    /// </summary>
    Task<LinqCompiledQuery> CompileQueryAsync(LinqModelAssembly model, string query, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>Contexte EF du modele sur une chaine de connexion SQL Server.</summary>
    DbContext CreateContext(LinqModelAssembly model, string connectionString);
}
