using Microsoft.Data.SqlClient;
using SqlToolManagerMaui.Models.Databases;

namespace SqlToolManagerMaui.Services.Databases;

/// <summary>Catalogue SQL Server lu dans les vues systeme (sys.databases, sys.objects).</summary>
[AutoRegister(As = typeof(IDatabaseSchemaReader))]
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

    // user_type_id : un type alias (CREATE TYPE) s'affiche sous son nom, comme dans SSMS.
    private const string ColumnsQuery = """
        SELECT c.name, t.name, c.max_length, c.precision, c.scale,
               c.is_nullable, c.is_identity, c.is_computed,
               CAST(CASE WHEN EXISTS (
                   SELECT 1 FROM sys.index_columns ic
                   INNER JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                   WHERE i.is_primary_key = 1 AND ic.object_id = c.object_id AND ic.column_id = c.column_id)
                   THEN 1 ELSE 0 END AS bit),
               CAST(CASE WHEN EXISTS (
                   SELECT 1 FROM sys.foreign_key_columns fk
                   WHERE fk.parent_object_id = c.object_id AND fk.parent_column_id = c.column_id)
                   THEN 1 ELSE 0 END AS bit)
        FROM sys.columns c
        INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(QUOTENAME(@schema) + N'.' + QUOTENAME(@name))
        ORDER BY c.column_id;
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

    public async Task<IReadOnlyList<DatabaseColumnEntry>> GetColumnsAsync(string connectionString, string database, string schema, string objectName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(database);

        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new SqlCommand(ColumnsQuery, connection);
        command.Parameters.Add(new SqlParameter("@schema", System.Data.SqlDbType.NVarChar, 128) { Value = schema });
        command.Parameters.Add(new SqlParameter("@name", System.Data.SqlDbType.NVarChar, 128) { Value = objectName });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<DatabaseColumnEntry>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var dataType = FormatType(reader.GetString(1), reader.GetInt16(2), reader.GetByte(3), reader.GetByte(4));
            result.Add(new DatabaseColumnEntry(
                reader.GetString(0),
                dataType,
                IsNullable: reader.GetBoolean(5),
                IsPrimaryKey: reader.GetBoolean(8),
                IsForeignKey: reader.GetBoolean(9),
                IsIdentity: reader.GetBoolean(6),
                IsComputed: reader.GetBoolean(7)));
        }

        return result;
    }

    /// <summary>Type affiche comme dans SSMS : longueur, precision ou echelle selon le type de base.</summary>
    private static string FormatType(string typeName, short maxLength, byte precision, byte scale)
    {
        string Length(int divisor) => maxLength == -1 ? "max" : (maxLength / divisor).ToString();

        return typeName.ToLowerInvariant() switch
        {
            "nvarchar" or "nchar" => $"{typeName}({Length(2)})",
            "varchar" or "char" or "varbinary" or "binary" => $"{typeName}({Length(1)})",
            "decimal" or "numeric" => $"{typeName}({precision},{scale})",
            "datetime2" or "time" or "datetimeoffset" => $"{typeName}({scale})",
            "float" when precision != 53 => $"{typeName}({precision})",
            _ => typeName,
        };
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
