using Querify.Databases.Models.Connections;
using Querify.Databases.Schemas;
using Quill.Code.SqlServer.Schema;
using Quill.Data.Sql;
using System.Data.Common;

namespace Querify.Languages.Sql.Editors;

/// <summary>
/// Catalogue de Quill sur le cache des schemas de l'application : vide tant que la connexion n'est pas ouverte,
/// et quand le serveur est injoignable ou les droits insuffisants.
/// </summary>
internal sealed class QuerifySqlCatalog : ISqlCatalog
{
    private readonly IDatabaseSchemaCache _cache;
    private readonly DatabaseConnectionInfo? _connection;

    public QuerifySqlCatalog(IDatabaseSchemaCache cache, DatabaseConnectionInfo? connection)
    {
        _cache = cache;
        _connection = connection;
    }

    public async Task<IReadOnlyList<SqlDatabaseInfo>> GetDatabasesAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not { IsConnected: true } connection)
            return [];

        try
        {
            return await _cache.GetDatabasesAsync(connection, cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException)
        {
            return [];
        }
    }

    public async Task<SqlDatabaseSchema?> GetDatabaseAsync(string? database, CancellationToken cancellationToken = default)
    {
        if (_connection is not { IsConnected: true } connection)
            return null;

        try
        {
            return await _cache.GetSchemaAsync(connection, database, cancellationToken);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException)
        {
            return null;
        }
    }
}
