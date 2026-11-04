using Querify.Databases.Common;
using Querify.Databases.Constants;
using Querify.Databases.Enums;
using Querify.Databases.Models.Connections;
using System.Data.Common;
using System.Diagnostics;

namespace Querify.Databases.Connections;

/// <summary>
/// Base des testeurs ADO.NET : analyse par le DbConnectionStringBuilder du pilote, puis ouverture avec un timeout court
/// (<see cref="ConnectionConstants.TestConnectTimeoutSeconds"/>). Un moteur ne fournit que son pilote.
/// </summary>
public abstract class DbConnectionTester : IConnectionTester
{
    public abstract DatabaseProvider Provider { get; }

    /// <summary>Mots-cles du serveur, cites quand la chaine n'en precise aucun (ex. « Server / Data Source »).</summary>
    protected abstract string ServerKeywords { get; }

    /// <summary>Analyse la chaine ; leve <see cref="ArgumentException"/> ou <see cref="FormatException"/> si elle est invalide.</summary>
    protected abstract DbConnectionStringBuilder Parse(string connectionString);

    /// <summary>Serveur et base affiches dans la liste ; serveur vide = chaine incomplete.</summary>
    protected abstract (string Server, string Database) Describe(DbConnectionStringBuilder builder);

    protected abstract void SetConnectTimeout(DbConnectionStringBuilder builder, int seconds);

    protected abstract DbConnection CreateConnection(string connectionString);

    /// <summary>Message d'echec d'ouverture ; par defaut celui du pilote.</summary>
    protected virtual string FormatError(DbException exception)
        => $"Echec {DatabaseProviders.GetLabel(Provider)} : {exception.Message}";

    public string? Validate(string connectionString, DatabaseConnectionInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        if (string.IsNullOrWhiteSpace(connectionString))
            return "La chaine de connexion est vide.";

        DbConnectionStringBuilder builder;
        try
        {
            builder = Parse(connectionString);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            return $"Chaine de connexion invalide : {ex.Message}";
        }

        var (server, database) = Describe(builder);
        if (string.IsNullOrWhiteSpace(server))
            return $"La chaine ne precise aucun serveur ({ServerKeywords}).";

        info.Server = server;
        info.Database = database;
        return null;
    }

    public async Task<ConnectionTestResult> TestAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        string effective;
        try
        {
            var builder = Parse(connectionString);
            SetConnectTimeout(builder, ConnectionConstants.TestConnectTimeoutSeconds);
            effective = builder.ConnectionString;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            return ConnectionTestResult.Fail($"Chaine de connexion invalide : {ex.Message}");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = CreateConnection(effective);
            await connection.OpenAsync(cancellationToken);

            stopwatch.Stop();
            var target = string.IsNullOrEmpty(connection.Database)
                ? connection.DataSource
                : $"{connection.DataSource} / {connection.Database}";

            return ConnectionTestResult.Ok(
                stopwatch.Elapsed,
                $"Connexion etablie a {target} ({DatabaseProviders.GetLabel(Provider)} {connection.ServerVersion}).");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbException ex)
        {
            stopwatch.Stop();
            return ConnectionTestResult.Fail(FormatError(ex), stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ConnectionTestResult.Fail($"Echec : {ex.Message}", stopwatch.Elapsed);
        }
    }
}
