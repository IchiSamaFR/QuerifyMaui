using Microsoft.Data.SqlClient;
using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Databases.Schemas;
using SqlToolManagerMaui.Core.Services.Databases;

namespace SqlToolManagerMaui.Languages.Sql.SqlServer;

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
        SELECT s.name, o.name,
               CONVERT(nvarchar(max), (SELECT ep.value FROM sys.extended_properties ep
                   WHERE ep.class = 1 AND ep.major_id = o.object_id AND ep.minor_id = 0 AND ep.name = N'MS_Description'))
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
                   THEN 1 ELSE 0 END AS bit),
               CONVERT(nvarchar(max), (SELECT ep.value FROM sys.extended_properties ep
                   WHERE ep.class = 1 AND ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = N'MS_Description'))
        FROM sys.columns c
        INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
        WHERE c.object_id = OBJECT_ID(QUOTENAME(@schema) + N'.' + QUOTENAME(@name))
        ORDER BY c.column_id;
        """;

    // Trois jeux de resultats : base courante, objets, colonnes (rattachees par object_id).
    private const string SnapshotQuery = """
        SELECT DB_NAME();

        SELECT o.object_id, s.name, o.name, RTRIM(o.type)
        FROM sys.objects o
        INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
        WHERE o.type IN ('U', 'V', 'P') AND o.is_ms_shipped = 0;

        SELECT c.object_id, c.name, t.name, c.max_length, c.precision, c.scale,
               CASE WHEN t.is_user_defined = 1 AND t.is_assembly_type = 0 THEN TYPE_NAME(t.system_type_id) ELSE t.name END,
               c.is_nullable,
               CAST(ISNULL(pk.key_ordinal, 0) AS int)
        FROM sys.columns c
        INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
        INNER JOIN sys.objects o ON o.object_id = c.object_id
        LEFT JOIN (
            SELECT ic.object_id, ic.column_id, ic.key_ordinal
            FROM sys.index_columns ic
            INNER JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            WHERE i.is_primary_key = 1
        ) pk ON pk.object_id = c.object_id AND pk.column_id = c.column_id
        WHERE o.type IN ('U', 'V') AND o.is_ms_shipped = 0
        ORDER BY c.object_id, c.column_id;
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
            result.Add(new DatabaseObjectEntry(reader.GetString(0), reader.GetString(1), ReadComment(reader, 2)));

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
                IsComputed: reader.GetBoolean(7),
                Comment: ReadComment(reader, 10)));
        }

        return result;
    }

    public async Task<DatabaseSchemaSnapshot> GetSchemaSnapshotAsync(string connectionString, string? database, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(connectionString, database, cancellationToken);
        await using var command = new SqlCommand(SnapshotQuery, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);
        var databaseName = reader.GetString(0);

        await reader.NextResultAsync(cancellationToken);
        var objects = new List<(int Id, string Schema, string Name, DatabaseObjectKind Kind)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var kind = reader.GetString(3) switch
            {
                "U" => DatabaseObjectKind.Table,
                "V" => DatabaseObjectKind.View,
                _ => DatabaseObjectKind.StoredProcedure,
            };
            objects.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), kind));
        }

        await reader.NextResultAsync(cancellationToken);
        var columns = new Dictionary<int, List<DatabaseSchemaColumn>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var objectId = reader.GetInt32(0);
            if (!columns.TryGetValue(objectId, out var list))
                columns[objectId] = list = [];

            var precision = reader.GetByte(4);
            var scale = reader.GetByte(5);
            list.Add(new DatabaseSchemaColumn(
                reader.GetString(1),
                FormatType(reader.GetString(2), reader.GetInt16(3), precision, scale),
                SqlType: reader.GetString(6),
                IsNullable: reader.GetBoolean(7),
                PrimaryKeyOrdinal: reader.GetInt32(8),
                Precision: precision,
                Scale: scale));
        }

        return new DatabaseSchemaSnapshot
        {
            Database = databaseName,
            Objects = objects.Select(o => new DatabaseSchemaObject(o.Schema, o.Name, o.Kind, columns.TryGetValue(o.Id, out var list) ? list : []))
                             .ToArray()
        };
    }

    private static string? ReadComment(SqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) || string.IsNullOrWhiteSpace(reader.GetString(ordinal)) ? null : reader.GetString(ordinal).Trim();

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
