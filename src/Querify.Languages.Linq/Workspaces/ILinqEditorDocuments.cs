using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Quill.Code.CSharp.Workspaces;

namespace Querify.Languages.Linq.Workspaces;

/// <summary>
/// Document Roslyn (Quill) d'un onglet LINQ pour l'autocompletion, le survol, les signatures et les erreurs :
/// la requete dans son contexte genere, avec le DbContext de la base quand il est disponible.
/// </summary>
public interface ILinqEditorDocuments
{
    /// <param name="database">Base interrogee ; null = base de la connexion.</param>
    /// <param name="parameters">Parametres du dashboard, declares en variables avant la requete.</param>
    Task<CSharpDocument> GetAsync(string query, DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters, CancellationToken cancellationToken = default);
}
