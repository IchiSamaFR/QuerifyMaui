using Querify.Databases.Enums;
using Quill.Code.Execution;

namespace Querify.Languages.Sql.Editors;

/// <summary>Type SQL sous lequel un parametre du dashboard est envoye a chaque moteur, affiche par l'editeur.</summary>
internal static class SqlParameterTypes
{
    public static string GetName(DatabaseProvider? provider, QueryParameterType type) => provider switch
    {
        DatabaseProvider.MySql => type switch
        {
            QueryParameterType.Integer => "bigint",
            QueryParameterType.Decimal => "decimal",
            QueryParameterType.Date => "date",
            QueryParameterType.DateTime => "datetime",
            QueryParameterType.Boolean => "tinyint(1)",
            QueryParameterType.Guid => "char(36)",
            _ => "varchar",
        },
        DatabaseProvider.PostgreSql => type switch
        {
            QueryParameterType.Integer => "bigint",
            QueryParameterType.Decimal => "numeric",
            QueryParameterType.Date => "date",
            QueryParameterType.DateTime => "timestamp",
            QueryParameterType.Boolean => "boolean",
            QueryParameterType.Guid => "uuid",
            _ => "text",
        },
        DatabaseProvider.Oracle => type switch
        {
            QueryParameterType.Integer or QueryParameterType.Decimal => "NUMBER",
            QueryParameterType.Date => "DATE",
            QueryParameterType.DateTime => "TIMESTAMP",
            QueryParameterType.Boolean => "NUMBER(1)",
            QueryParameterType.Guid => "RAW(16)",
            _ => "VARCHAR2",
        },
        DatabaseProvider.Sqlite => type switch
        {
            QueryParameterType.Integer or QueryParameterType.Boolean => "INTEGER",
            QueryParameterType.Decimal => "REAL",
            _ => "TEXT",
        },
        _ => type switch
        {
            QueryParameterType.Integer => "int",
            QueryParameterType.Decimal => "decimal",
            QueryParameterType.Date => "date",
            QueryParameterType.DateTime => "datetime2",
            QueryParameterType.Boolean => "bit",
            QueryParameterType.Guid => "uniqueidentifier",
            _ => "nvarchar",
        },
    };
}
