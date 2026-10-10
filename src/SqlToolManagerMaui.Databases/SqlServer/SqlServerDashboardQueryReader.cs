using Microsoft.Data.SqlClient;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Databases.Dashboards;
using System.Data;
using System.Globalization;

namespace SqlToolManagerMaui.Databases.SqlServer;

/// <summary>Cartes du dashboard sur SQL Server : la requete est envoyee telle quelle, les parametres en SqlParameter types.</summary>
[AutoRegister(As = typeof(IDashboardQueryReader))]
public sealed class SqlServerDashboardQueryReader : IDashboardQueryReader
{
    private const int ConnectTimeoutSeconds = 8;
    private const int CommandTimeoutSeconds = 120;
    private const int MaxBinaryBytesDisplayed = 32;
    private const string UnnamedColumn = "(Aucun nom de colonne)";

    /// <summary>« Must declare the scalar variable ».</summary>
    private const int UndeclaredVariableError = 137;

    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public async Task<QueryResultSet> ReadAsync(string connectionString, string? database, string query, IReadOnlyList<DashboardQueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(parameters);

        var builder = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = ConnectTimeoutSeconds };
        if (!string.IsNullOrWhiteSpace(database))
            builder.InitialCatalog = database;

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandTimeout = CommandTimeoutSeconds;
        foreach (var parameter in parameters)
            command.Parameters.Add(CreateParameter(parameter));

        SqlDataReader reader;
        try
        {
            reader = await command.ExecuteReaderAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == UndeclaredVariableError)
        {
            // Variable inconnue : on liste ce qui a reellement ete envoye pour qu'un nom mal saisi se voie.
            var sent = parameters.Count == 0
                ? "aucun"
                : string.Join(", ", command.Parameters.Cast<SqlParameter>().Select(p => p.ParameterName));
            throw new InvalidOperationException($"{ex.Message} Paramètres du dashboard envoyés : {sent}.", ex);
        }

        await using var _ = reader;

        // Premier jeu qui a des colonnes : un DECLARE ou un SET avant le SELECT n'en produit pas.
        while (reader.FieldCount == 0 && await reader.NextResultAsync(cancellationToken))
        {
        }

        if (reader.FieldCount == 0)
            return new QueryResultSet(1, [], [], isTruncated: false);

        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(i => new QueryResultColumn(string.IsNullOrEmpty(reader.GetName(i)) ? UnnamedColumn : reader.GetName(i), reader.GetDataTypeName(i)))
            .ToList();

        var unsupported = new bool[reader.FieldCount];
        List<QueryResultRow> rows = [];
        var isTruncated = false;

        while (await reader.ReadAsync(cancellationToken))
        {
            if (rows.Count == maxRows)
            {
                isTruncated = true;
                break;
            }

            var cells = new string?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
                cells[i] = ReadCell(reader, i, unsupported);

            rows.Add(new QueryResultRow(rows.Count + 1, cells));
        }

        return new QueryResultSet(1, columns, rows, isTruncated);
    }

    private static SqlParameter CreateParameter(DashboardQueryParameter parameter)
    {
        var (type, size) = parameter.Type switch
        {
            DashboardParameterType.Integer => (SqlDbType.BigInt, 0),
            DashboardParameterType.Decimal => (SqlDbType.Decimal, 0),
            DashboardParameterType.Date => (SqlDbType.Date, 0),
            DashboardParameterType.DateTime => (SqlDbType.DateTime2, 0),
            DashboardParameterType.Boolean => (SqlDbType.Bit, 0),
            DashboardParameterType.Guid => (SqlDbType.UniqueIdentifier, 0),
            _ => (SqlDbType.NVarChar, -1),
        };

        // Un nom saisi a la main dans le JSON peut garder son @ ou des espaces.
        var result = new SqlParameter($"@{parameter.Name.Trim().TrimStart('@')}", type) { Value = parameter.Value ?? DBNull.Value };
        if (size != 0)
            result.Size = size;
        return result;
    }

    private static string? ReadCell(SqlDataReader reader, int ordinal, bool[] unsupported)
    {
        if (!unsupported[ordinal])
        {
            try
            {
                var value = reader.GetValue(ordinal);
                return value is DBNull ? null : Format(value);
            }
            catch (Exception ex) when (ex is InvalidCastException or FileNotFoundException or NotSupportedException)
            {
                // Types CLR (geography, hierarchyid...) dont l'assembly n'est pas disponible.
                unsupported[ordinal] = true;
            }
        }

        return $"<{reader.GetDataTypeName(ordinal)}>";
    }

    private static string? Format(object value) => value switch
    {
        string s => s,
        bool b => b ? "1" : "0",
        DateTime d => d.TimeOfDay == TimeSpan.Zero
            ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
        byte[] bytes => bytes.Length > MaxBinaryBytesDisplayed
            ? $"0x{Convert.ToHexString(bytes, 0, MaxBinaryBytesDisplayed)}…"
            : $"0x{Convert.ToHexString(bytes)}",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };
}
