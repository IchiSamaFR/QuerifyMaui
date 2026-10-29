using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Services.Editors.Highlighting;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;
using Querify.Databases.Schemas;
using Querify.Languages.Commons.Editors;
using Querify.Languages.Sql.Analysis;
using Querify.Languages.Sql.Completion;
using Quill.Code;
using Quill.Code.Sql;

namespace Querify.Languages.Sql.Editors;

/// <summary>
/// Editeur T-SQL : <see cref="SqlLanguageService"/> de Quill (fautes de syntaxe sans connexion), complete du schema de la
/// connexion pour l'autocompletion et le survol.
/// </summary>
[AutoRegister(As = typeof(IEditorLanguage))]
public sealed class SqlEditorLanguage : IEditorLanguage
{
    private readonly IDatabaseSchemaCache _cache;
    private readonly SqlKeywordCatalog _keywords;

    public SqlEditorLanguage(IDatabaseSchemaCache cache, ISyntaxConfigurationStore configurations)
    {
        _cache = cache;
        _keywords = new SqlKeywordCatalog(configurations);
    }

    public SyntaxLanguage Language => SyntaxLanguage.Sql;

    public ICodeLanguageService CreateService(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters)
        => new SqlLanguageService(
            completion: new SqlEditorCompletion(_keywords, new SqlSchemaCompletion(_cache, _keywords), connection, database, parameters),
            hover: new SqlSchemaHover(_cache, connection, database, parameters));

    /// <summary>Mots-cles, catalogue de la base par defaut et liste des bases : charges des la connexion, pas a la premiere frappe.</summary>
    public async Task WarmUpAsync(DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        try
        {
            await _keywords.GetItemsAsync(cancellationToken);

            if (connection is { IsConnected: true })
                await Task.WhenAll(_cache.GetSchemaAsync(connection, null, cancellationToken), _cache.GetDatabasesAsync(connection, cancellationToken));
        }
        catch (Exception)
        {
            // Prechargement opportuniste : la vraie demande retentera et gerera l'erreur.
        }
    }
}
