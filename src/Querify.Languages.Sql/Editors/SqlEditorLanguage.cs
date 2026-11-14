using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Databases.Enums;
using Querify.Databases.Models.Connections;
using Querify.Databases.Schemas;
using Querify.Languages.Editors;
using Querify.Languages.Queries;
using Quill.Code;
using Quill.Code.Sql.SqlServer;
using Quill.Code.Execution;

namespace Querify.Languages.Sql.Editors;

/// <summary>
/// Editeur SQL : <see cref="SqlLanguageService"/> de Quill sur le catalogue de la connexion de l'onglet
/// (<see cref="QuerifySqlCatalog"/>), dans le dialecte de son moteur (<see cref="SqlDialects"/>), avec les parametres
/// du dashboard comme variables.
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
            Dialect = SqlDialects.For(connection?.Provider),
            Catalog = new QuerifySqlCatalog(_cache, connection),
            Database = database,
            Variables = [.. parameters.Select(p => new SqlVariable(p.Name, SqlParameterTypes.GetName(connection?.Provider, p.Type), "Paramètre du dashboard"))],
            // Le controle de syntaxe (ScriptDom) n'existe que pour T-SQL : il signalerait a tort LIMIT, les backticks... des autres moteurs.
            CheckSyntax = connection is null || connection.Provider == DatabaseProvider.SqlServer,
        });

    /// <summary>Catalogue de la base par defaut et liste des bases : charges des la connexion, pas a la premiere frappe.</summary>
    public async Task WarmUpAsync(DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        var catalog = new QuerifySqlCatalog(_cache, connection);
        await Task.WhenAll(catalog.GetDatabaseAsync(null, cancellationToken), catalog.GetDatabasesAsync(cancellationToken));
    }

    /// <summary>Nom complet et lecture des premieres lignes dans le dialecte du moteur : TOP, LIMIT ou FETCH FIRST.</summary>
    public string CreateSelectFirstRows(InsertQueryMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var dialect = SqlDialects.For(message.Connection.Provider);
        var table = dialect.FormatObjectName(message.Database, message.Schema, message.Table, alwaysQuote: true);
        return dialect.SelectFirstRows(table, message.RowCount);
    }
}
