using MySqlConnector;
using Querify.Databases.Connections;
using Querify.Databases.Enums;
using System.Data.Common;

namespace Querify.Databases.MySql;

/// <summary>Testeur MySQL / MariaDB (MySqlConnector).</summary>
public sealed class MySqlConnectionTester : DbConnectionTester
{
    public override DatabaseProvider Provider => DatabaseProvider.MySql;

    protected override string ServerKeywords => "Server / Host";

    protected override DbConnectionStringBuilder Parse(string connectionString) => new MySqlConnectionStringBuilder(connectionString);

    protected override (string Server, string Database) Describe(DbConnectionStringBuilder builder)
    {
        var mySql = (MySqlConnectionStringBuilder)builder;
        return (mySql.Server, mySql.Database);
    }

    protected override void SetConnectTimeout(DbConnectionStringBuilder builder, int seconds)
        => ((MySqlConnectionStringBuilder)builder).ConnectionTimeout = (uint)seconds;

    protected override DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    protected override string FormatError(DbException exception)
        => exception is MySqlException mySql ? $"Echec MySQL ({mySql.Number}) : {mySql.Message}" : base.FormatError(exception);
}
