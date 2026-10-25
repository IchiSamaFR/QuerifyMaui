using Microsoft.EntityFrameworkCore;
using SqlToolManagerMaui.Languages.Linq.Modeling;
using SqlToolManagerMaui.Languages.Linq.Queries;

namespace SqlToolManagerMaui.Languages.Linq.Compilation;

/// <summary>
/// Compilation Roslyn en memoire : le DbContext genere (une fois par schema), puis chaque
/// requete LINQ, enveloppee dans une methode qui recoit le contexte sous le nom de la base.
/// </summary>
public interface ILinqCompiler
{
    /// <summary>Compile et charge le modele genere ; leve <see cref="LinqCompilationException"/> en cas d'echec.</summary>
    LinqModelAssembly CompileModel(EfGeneratedModel model);

    /// <summary>
    /// Compile une expression LINQ ; les erreurs sont rapportees, pas levees. Les <paramref name="parameters"/>
    /// sont declares en variables avant la requete, leurs valeurs fixees pour l'execution.
    /// </summary>
    LinqCompiledQuery CompileQuery(LinqModelAssembly model, string query, IReadOnlyList<LinqParameter>? parameters = null);

    /// <summary>Contexte EF du modele sur une chaine de connexion SQL Server.</summary>
    DbContext CreateContext(LinqModelAssembly model, string connectionString);
}
