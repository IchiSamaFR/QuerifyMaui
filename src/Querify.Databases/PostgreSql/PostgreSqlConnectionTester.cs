using Npgsql;
using Querify.Databases.Connections;
using Querify.Databases.Enums;
using System.Data.Common;

namespace Querify.Databases.PostgreSql;

/// <summary>Testeur PostgreSQL (Npgsql).</summary>
public sealed class PostgreSqlConnectionTester : DbConnectionTester
{
    public override DatabaseProvider Provider => DatabaseProvider.PostgreSql;

    protected override string ServerKeywords => "Host / Server";

    protected override DbConnectionStringBuilder Parse(string connectionString) => new NpgsqlConnectionStringBuilder(connectionString);

    protected override (string Server, string Database) Describe(DbConnectionStringBuilder builder)
    {
        var postgres = (NpgsqlConnectionStringBuilder)builder;
        return (postgres.Host ?? string.Empty, postgres.Database ?? string.Empty);
    }

    protected override void SetConnectTimeout(DbConnectionStringBuilder builder, int seconds)
        => ((NpgsqlConnectionStringBuilder)builder).Timeout = seconds;

    protected override DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

    protected override string FormatError(DbException exception)
        => exception is PostgresException postgres ? $"Echec PostgreSQL ({postgres.SqlState}) : {postgres.MessageText}" : base.FormatError(exception);
}
