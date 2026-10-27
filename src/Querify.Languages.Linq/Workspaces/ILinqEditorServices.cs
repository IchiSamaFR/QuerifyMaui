using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Workspaces;

/// <summary>
/// Service C# (Quill) d'une demande de l'editeur LINQ : DbContext de la base quand il est disponible,
/// parametres du dashboard.
/// </summary>
public interface ILinqEditorServices
{
    /// <param name="database">Base interrogee ; null = base de la connexion.</param>
    /// <param name="parameters">Parametres du dashboard, declares en variables avant la requete.</param>
    Task<ICSharpLanguageService> GetAsync(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters, CancellationToken cancellationToken = default);
}
