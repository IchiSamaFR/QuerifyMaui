using Oracle.ManagedDataAccess.Client;
using Querify.Databases.Connections;
using Querify.Databases.Enums;
using System.Data.Common;

namespace Querify.Databases.Oracle;

/// <summary>Testeur Oracle (ODP.NET Core) ; la « base » affichee est le schema de l'utilisateur.</summary>
public sealed class OracleConnectionTester : DbConnectionTester
{
    public override DatabaseProvider Provider => DatabaseProvider.Oracle;

    protected override string ServerKeywords => "Data Source";

    protected override DbConnectionStringBuilder Parse(string connectionString) => new OracleConnectionStringBuilder(connectionString);

    protected override (string Server, string Database) Describe(DbConnectionStringBuilder builder)
    {
        var oracle = (OracleConnectionStringBuilder)builder;
        return (oracle.DataSource ?? string.Empty, oracle.UserID ?? string.Empty);
    }

    protected override void SetConnectTimeout(DbConnectionStringBuilder builder, int seconds)
        => ((OracleConnectionStringBuilder)builder).ConnectionTimeout = seconds;

    protected override DbConnection CreateConnection(string connectionString) => new OracleConnection(connectionString);
}
