using Microsoft.Data.SqlClient;
using SqlToolManagerMaui.Models.Databases;

namespace SqlToolManagerMaui.Services.Databases;

/// <summary>Catalogue SQL Server lu dans les vues systeme (sys.databases, sys.objects).</summary>
public sealed class SqlServerSchemaReader : IDatabaseSchemaReader
{
    private const int ConnectTimeoutSeconds = 8;

    private const string DatabasesQuery = """
        SELECT name, CAST(CASE WHEN database_id <= 4 THEN 1 ELSE 0 END AS bit)
        FROM sys.databases
        WHERE state = 0
        ORDER BY name;
        """;

    // sys.objects.type est un char(2) : 'U' correspond a 'U ' (espaces de fin ignores).
    private const string ObjectsQuery = """
        SELECT s.name, o.name
        FROM sys.objects o
        INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.type = @type AND o.is_ms_shipped = 0
        ORDER BY s.name, o.name;
        """;

    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public async Task<IReadOnlyList<DatabaseCatalogEntry>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, database: null, cancellationToken);
        await using var command = new SqlCommand(DatabasesQuery, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<DatabaseCatalogEntry>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new DatabaseCatalogEntry(reader.GetString(0), reader.GetBoolean(1)));

        return result;
    }

    public async Task<IReadOnlyList<DatabaseObjectEntry>> GetObjectsAsync(string connectionString, string database, DatabaseObjectKind kind, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new SqlCommand(ObjectsQuery, connection);
        command.Parameters.Add(new SqlParameter("@type", System.Data.SqlDbType.Char, 2) { Value = TypeCode(kind) });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<DatabaseObjectEntry>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new DatabaseObjectEntry(reader.GetString(0), reader.GetString(1)));

        return result;
    }

    private static string TypeCode(DatabaseObjectKind kind) => kind switch
    {
        DatabaseObjectKind.Table => "U",
        DatabaseObjectKind.View => "V",
        DatabaseObjectKind.StoredProcedure => "P",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static async Task<SqlConnection> OpenAsync(string connectionString, string? database, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = ConnectTimeoutSeconds,
        };
        if (database is not null)
            builder.InitialCatalog = database;

        var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
