using Microsoft.Data.SqlClient;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Databases.Enums;
using SqlToolManagerMaui.Databases.Models.Queries;
using SqlToolManagerMaui.Languages.Commons.Queries;
using SqlToolManagerMaui.Languages.Sql.Queries;
using System.Diagnostics;
using System.Globalization;

namespace SqlToolManagerMaui.Languages.Sql.SqlServer;

/// <summary>
/// Executeur SQL Server, calque sur le comportement de SSMS : decoupage sur GO, un lot en
/// echec n'empeche pas les suivants, messages PRINT et nombre de lignes affectees remontes.
/// Une connexion est ouverte pour chaque execution.
/// </summary>
[AutoRegister(As = typeof(ISqlProviderExecutor))]
public sealed class SqlServerQueryExecutor : ISqlProviderExecutor
{
    private const int MaxBinaryBytesDisplayed = 64;
    private const string ApplicationName = "SqlToolManagerMaui";

    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public async Task<QueryExecutionResult> ExecuteAsync(string connectionString, string script, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(script);

        var resultSets = new List<QueryResultSet>();
        var messages = new List<QueryMessage>();
        var hasErrors = false;
        var wasCancelled = false;
        var stopwatch = Stopwatch.StartNew();

        var batches = SqlBatchSplitter.Split(script);
        if (batches.Count == 0)
        {
            messages.Add(new QueryMessage("Aucune instruction à exécuter."));
            return new QueryExecutionResult(resultSets, messages, TimeSpan.Zero, hasErrors, wasCancelled);
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString) { ApplicationName = ApplicationName };

            await using var connection = new SqlConnection(builder.ConnectionString);
            connection.InfoMessage += (_, e) =>
            {
                foreach (SqlError error in e.Errors)
                    messages.Add(new QueryMessage(error.Message));
            };

            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            foreach (var batch in batches)
            {
                for (var run = 0; run < batch.RepeatCount; run++)
                {
                    try
                    {
                        await ExecuteBatchAsync(connection, batch, resultSets, messages, cancellationToken).ConfigureAwait(false);
                    }
                    catch (SqlException ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        // Comme SSMS : le lot suivant est tout de meme execute.
                        hasErrors = true;
                        AddErrors(messages, ex, batch.StartLine);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }
        catch (SqlException) when (cancellationToken.IsCancellationRequested)
        {
            // L'annulation d'une commande en cours remonte sous forme de SqlException.
            wasCancelled = true;
        }
        catch (SqlException ex)
        {
            hasErrors = true;
            AddErrors(messages, ex, batchStartLine: null);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or InvalidOperationException)
        {
            hasErrors = true;
            messages.Add(new QueryMessage($"Échec : {ex.Message}", IsError: true));
        }

        stopwatch.Stop();

        if (wasCancelled)
            messages.Add(new QueryMessage("Requête annulée par l'utilisateur.", IsError: true));

        messages.Add(new QueryMessage($"Heure de fin : {DateTime.Now:yyyy-MM-dd HH:mm:ss} (durée {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff})"));

        return new QueryExecutionResult(resultSets, messages, stopwatch.Elapsed, hasErrors, wasCancelled);
    }

    private static async Task ExecuteBatchAsync(SqlConnection connection, SqlBatch batch, List<QueryResultSet> resultSets, List<QueryMessage> messages, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = batch.Text;
        // Pas de timeout : l'utilisateur garde la main via l'annulation.
        command.CommandTimeout = 0;
        command.StatementCompleted += (_, e) => messages.Add(new QueryMessage($"({e.RecordCount} ligne(s) affectée(s))"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        do
        {
            // FieldCount == 0 : instruction sans jeu de resultats (INSERT, UPDATE, DDL...).
            if (reader.FieldCount > 0)
                resultSets.Add(await ReadResultSetAsync(reader, resultSets.Count + 1, cancellationToken).ConfigureAwait(false));
        }
        while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
    }

    private static async Task<QueryResultSet> ReadResultSetAsync(SqlDataReader reader, int index, CancellationToken cancellationToken)
    {
        var fieldCount = reader.FieldCount;
        var columns = new QueryResultColumn[fieldCount];
        for (var i = 0; i < fieldCount; i++)
        {
            var name = reader.GetName(i);
            columns[i] = new QueryResultColumn(
                string.IsNullOrEmpty(name) ? "(Aucun nom de colonne)" : name,
                reader.GetDataTypeName(i));
        }

        var rows = new List<QueryResultRow>();
        var unsupportedColumns = new bool[fieldCount];
        var isTruncated = false;

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (rows.Count == QueryExecutionLimits.MaxRowsPerResultSet)
            {
                isTruncated = true;
                break;
            }

            var cells = new string?[fieldCount];
            for (var i = 0; i < fieldCount; i++)
                cells[i] = ReadCell(reader, i, unsupportedColumns);

            rows.Add(new QueryResultRow(rows.Count + 1, cells));
        }

        return new QueryResultSet(index, columns, rows, isTruncated);
    }

    private static string? ReadCell(SqlDataReader reader, int ordinal, bool[] unsupportedColumns)
    {
        if (unsupportedColumns[ordinal])
            return $"<{reader.GetDataTypeName(ordinal)}>";

        object value;
        try
        {
            value = reader.GetValue(ordinal);
        }
        catch (Exception ex) when (ex is InvalidCastException or FileNotFoundException or NotSupportedException)
        {
            // Types CLR (geography, hierarchyid...) dont l'assembly n'est pas disponible.
            unsupportedColumns[ordinal] = true;
            return $"<{reader.GetDataTypeName(ordinal)}>";
        }

        return Format(value);
    }

    private static string? Format(object value) => value switch
    {
        DBNull => null,
        string s => s,
        bool b => b ? "1" : "0",
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz", CultureInfo.InvariantCulture),
        byte[] bytes => FormatBinary(bytes),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    private static string FormatBinary(byte[] bytes)
    {
        var shown = Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, MaxBinaryBytesDisplayed));
        return bytes.Length > MaxBinaryBytesDisplayed ? $"0x{shown}…" : $"0x{shown}";
    }

    private static void AddErrors(List<QueryMessage> messages, SqlException exception, int? batchStartLine)
    {
        foreach (SqlError error in exception.Errors)
        {
            // Le serveur numerote les lignes a partir du debut du lot : on recale sur le script.
            var header = batchStartLine is { } start && error.LineNumber > 0
                ? $"Msg {error.Number}, Niveau {error.Class}, État {error.State}, Ligne {start + error.LineNumber - 1}"
                : $"Msg {error.Number}, Niveau {error.Class}, État {error.State}";

            messages.Add(new QueryMessage($"{header}{Environment.NewLine}{error.Message}", IsError: true));
        }
    }
}
