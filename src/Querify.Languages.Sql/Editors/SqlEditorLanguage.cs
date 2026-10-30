using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Models.Connections;
using Querify.Databases.Schemas;
using Querify.Languages.Commons.Editors;
using Quill.Code;
using Quill.Code.SqlServer;
using Quill.Code.SqlServer.Execution;

namespace Querify.Languages.Sql.Editors;

/// <summary>
/// Editeur T-SQL : <see cref="SqlLanguageService"/> de Quill sur le catalogue de la connexion de l'onglet
/// (<see cref="QuerifySqlCatalog"/>), avec les parametres du dashboard comme variables.
/// </summary>
[AutoRegister(As = typeof(IEditorLanguage))]
public sealed class SqlEditorLanguage : IEditorLanguage
{
    private readonly IDatabaseSchemaCache _cache;

    public SqlEditorLanguage(IDatabaseSchemaCache cache)
    {
        _cache = cache;
    }

    public SyntaxLanguage Language => SyntaxLanguage.Sql;

    public ICodeLanguageService CreateService(DatabaseConnectionInfo? connection, string? database, IReadOnlyList<QueryParameter> parameters)
        => new SqlLanguageService(new SqlLanguageOptions
        {
            Catalog = new QuerifySqlCatalog(_cache, connection),
            Database = database,
            Variables = [.. parameters.Select(p => new SqlVariable(p.Name, SqlTypeName(p.Type), "Paramètre du dashboard"))],
        });

    /// <summary>Catalogue de la base par defaut et liste des bases : charges des la connexion, pas a la premiere frappe.</summary>
    public async Task WarmUpAsync(DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        var catalog = new QuerifySqlCatalog(_cache, connection);
        await Task.WhenAll(catalog.GetDatabaseAsync(null, cancellationToken), catalog.GetDatabasesAsync(cancellationToken));
    }

    /// <summary>Type SQL sous lequel le parametre est envoye.</summary>
    private static string SqlTypeName(QueryParameterType type) => type switch
    {
        QueryParameterType.Integer => "int",
        QueryParameterType.Decimal => "decimal",
        QueryParameterType.Date => "date",
        QueryParameterType.DateTime => "datetime2",
        QueryParameterType.Boolean => "bit",
        QueryParameterType.Guid => "uniqueidentifier",
        _ => "nvarchar",
    };
}
