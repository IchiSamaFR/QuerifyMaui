using Microsoft.Data.Sqlite;
using Querify.Databases.Connections;
using Querify.Databases.Enums;
using System.Data.Common;

namespace Querify.Databases.Sqlite;

/// <summary>Testeur SQLite (Microsoft.Data.Sqlite) : le « serveur » est le fichier de la base.</summary>
public sealed class SqliteConnectionTester : DbConnectionTester
{
    public override DatabaseProvider Provider => DatabaseProvider.Sqlite;

    protected override string ServerKeywords => "Data Source";

    protected override DbConnectionStringBuilder Parse(string connectionString) => new SqliteConnectionStringBuilder(connectionString);

    protected override (string Server, string Database) Describe(DbConnectionStringBuilder builder)
        => (((SqliteConnectionStringBuilder)builder).DataSource ?? string.Empty, string.Empty);

    // Pas de delai d'ouverture : un fichier local s'ouvre ou echoue tout de suite.
    protected override void SetConnectTimeout(DbConnectionStringBuilder builder, int seconds)
    {
    }

    protected override DbConnection CreateConnection(string connectionString)
    {
        // Sans Mode explicite, SQLite creerait un fichier vide a un chemin errone : le test doit echouer.
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (builder.Mode == SqliteOpenMode.ReadWriteCreate)
            builder.Mode = SqliteOpenMode.ReadWrite;

        return new SqliteConnection(builder.ConnectionString);
    }
}
