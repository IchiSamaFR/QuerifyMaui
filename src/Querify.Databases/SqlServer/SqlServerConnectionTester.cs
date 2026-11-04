using Microsoft.Data.SqlClient;
using Querify.Databases.Connections;
using Querify.Databases.Enums;
using System.Data.Common;

namespace Querify.Databases.SqlServer;

/// <summary>Testeur SQL Server (Microsoft.Data.SqlClient).</summary>
public sealed class SqlServerConnectionTester : DbConnectionTester
{
    public override DatabaseProvider Provider => DatabaseProvider.SqlServer;

    protected override string ServerKeywords => "Server / Data Source";

    protected override DbConnectionStringBuilder Parse(string connectionString) => new SqlConnectionStringBuilder(connectionString);

    protected override (string Server, string Database) Describe(DbConnectionStringBuilder builder)
    {
        var sql = (SqlConnectionStringBuilder)builder;
        return (sql.DataSource, sql.InitialCatalog);
    }

    protected override void SetConnectTimeout(DbConnectionStringBuilder builder, int seconds)
        => ((SqlConnectionStringBuilder)builder).ConnectTimeout = seconds;

    protected override DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    protected override string FormatError(DbException exception)
        => exception is SqlException sql ? $"Echec SQL Server ({sql.Number}) : {sql.Message}" : base.FormatError(exception);
}
