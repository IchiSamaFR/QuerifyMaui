using Querify.Databases.Enums;
using Quill.Code.Sql.Dialects;
using Quill.Code.Sql.MySql;
using Quill.Code.Sql.Oracle;
using Quill.Code.Sql.PostgreSql;
using Quill.Code.Sql.Sqlite;
using Quill.Code.Sql.SqlServer;

namespace Querify.Languages.Sql.Editors;

/// <summary>Dialecte SQL de Quill par moteur de connexion : delimiteurs, parametres, premieres lignes, mots proposes.</summary>
internal static class SqlDialects
{
    /// <summary>Un moteur = une entree.</summary>
    private static readonly Dictionary<DatabaseProvider, SqlDialect> Dialects = new()
    {
        [DatabaseProvider.SqlServer] = SqlServerDialect.Instance,
        [DatabaseProvider.MySql] = MySqlDialect.Instance,
        [DatabaseProvider.PostgreSql] = PostgreSqlDialect.Instance,
        [DatabaseProvider.Oracle] = OracleDialect.Instance,
        [DatabaseProvider.Sqlite] = SqliteDialect.Instance,
    };

    /// <summary>Dialecte du moteur ; T-SQL sans connexion.</summary>
    public static SqlDialect For(DatabaseProvider? provider)
        => provider is { } known && Dialects.TryGetValue(known, out var dialect) ? dialect : SqlServerDialect.Instance;
}
