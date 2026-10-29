using Querify.Databases.Enums;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Schemas;
using Querify.Databases.Schemas;
using Quill.Code.Sql.Schema;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Querify.Languages.Sql.Editors;

/// <summary>
/// Catalogue de Quill sur le cache des schemas de l'application : vide tant que la connexion n'est pas ouverte,
/// et quand le serveur est injoignable ou les droits insuffisants.
/// </summary>
internal sealed class QuerifySqlCatalog : ISqlCatalog
{
    // Une conversion par catalogue charge : le meme instantane sert a toutes les frappes et a tous les onglets.
    private static readonly ConditionalWeakTable<DatabaseSchemaSnapshot, SqlDatabaseSchema> Converted = new();

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
            var databases = await _cache.GetDatabasesAsync(connection, cancellationToken);
            return databases.Select(d => new SqlDatabaseInfo(d.Name, d.IsSystem)).ToArray();
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
            var snapshot = await _cache.GetSchemaAsync(connection, database, cancellationToken);
            return Converted.GetValue(snapshot, Convert);
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException or TimeoutException)
        {
            return null;
        }
    }

    private static SqlDatabaseSchema Convert(DatabaseSchemaSnapshot snapshot)
        => new(snapshot.Database, snapshot.Objects
            .Select(o => new SqlSchemaObject(
                o.Schema,
                o.Name,
                o.Kind switch
                {
                    DatabaseObjectKind.View => SqlSchemaObjectKind.View,
                    DatabaseObjectKind.StoredProcedure => SqlSchemaObjectKind.Procedure,
                    _ => SqlSchemaObjectKind.Table,
                },
                o.Columns.Select(c => new SqlColumn(c.Name, c.DataType, c.IsNullable, c.IsPrimaryKey)).ToArray()))
            .ToArray());
}
