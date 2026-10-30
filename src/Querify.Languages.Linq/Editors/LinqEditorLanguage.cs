using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Models.Connections;
using Querify.Languages.Commons.Editors;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using Quill.Code;
using Quill.Code.SqlServer.Execution;

namespace Querify.Languages.Linq.Editors;

/// <summary>
/// Editeur des onglets LINQ : <see cref="Quill.Code.CSharp.CSharpLanguageService"/> de Quill, Roslyn analysant la requete
/// contre le contexte genere (Db. -> tables, u. -> colonnes, .Wh -> Where).
/// </summary>
[AutoRegister(As = typeof(IEditorLanguage))]
public sealed class LinqEditorLanguage : IEditorLanguage
{
    private readonly ILinqContexts _contexts;

    public LinqEditorLanguage(ILinqContexts contexts)
    {
        _contexts = contexts;
    }

    public SyntaxLanguage Language => SyntaxLanguage.Linq;

    /// <summary>Parametres vus du script : memes noms et types qu'a l'execution, sans les valeurs (le service Roslyn reste stable).</summary>
    public ICodeLanguageService CreateService(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters)
        => new LinqEditorService(_contexts, connection, database, [.. parameters.Select(p => LinqParameter.From(p) with { Value = null })]);

    /// <summary>Composition MEF de Roslyn (1 a 2 s), JIT de la completion et compilation du DbContext genere : faits a l'ouverture de l'onglet.</summary>
    public async Task WarmUpAsync(DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await _contexts.GetLanguageServiceAsync(connection, database: null, cancellationToken: cancellationToken);
            await service.WarmUpAsync(cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            // Prechargement opportuniste : la vraie demande retentera et gerera l'erreur.
        }
    }
}
