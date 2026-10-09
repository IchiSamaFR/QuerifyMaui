using Microsoft.Data.SqlClient;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Services.Databases.Connections;
using System.Diagnostics;

namespace SqlToolManagerMaui.Services.Databases;

/// <summary>
/// Testeur SQL Server : s'appuie sur <see cref="SqlConnectionStringBuilder"/> pour
/// l'analyse, puis ouvre la connexion avec un timeout court afin que le bouton
/// "Tester" ne bloque pas l'UI pendant 30 secondes.
/// </summary>
[AutoRegister(As = typeof(IConnectionTester))]
public sealed class SqlServerConnectionTester : IConnectionTester
{
    private const int TestConnectTimeoutSeconds = 8;

    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public string? Validate(string connectionString, DatabaseConnectionInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        if (string.IsNullOrWhiteSpace(connectionString))
            return "La chaine de connexion est vide.";

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException ex)
        {
            return $"Chaine de connexion invalide : {ex.Message}";
        }
        catch (FormatException ex)
        {
            return $"Chaine de connexion invalide : {ex.Message}";
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource))
            return "La chaine ne precise aucun serveur (Server / Data Source).";

        info.Server = builder.DataSource;
        info.Database = builder.InitialCatalog;
        return null;
    }

    public async Task<ConnectionTestResult> TestAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        string effective;
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = TestConnectTimeoutSeconds,
            };
            effective = builder.ConnectionString;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            return ConnectionTestResult.Fail($"Chaine de connexion invalide : {ex.Message}");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(effective);
            await connection.OpenAsync(cancellationToken);

            stopwatch.Stop();
            var target = string.IsNullOrEmpty(connection.Database)
                ? connection.DataSource
                : $"{connection.DataSource} / {connection.Database}";

            return ConnectionTestResult.Ok(
                stopwatch.Elapsed,
                $"Connexion etablie a {target} (SQL Server {connection.ServerVersion}).");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex)
        {
            stopwatch.Stop();
            return ConnectionTestResult.Fail($"Echec SQL Server ({ex.Number}) : {ex.Message}", stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ConnectionTestResult.Fail($"Echec : {ex.Message}", stopwatch.Elapsed);
        }
    }
}
