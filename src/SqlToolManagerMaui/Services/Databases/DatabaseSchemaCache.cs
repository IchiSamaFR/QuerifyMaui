using System.Collections.Concurrent;
using SqlToolManagerMaui.Models.Databases;

namespace SqlToolManagerMaui.Services.Databases;

/// <inheritdoc cref="IDatabaseSchemaCache"/>
[AutoRegister(As = typeof(IDatabaseSchemaCache))]
public sealed class DatabaseSchemaCache : IDatabaseSchemaCache
{
    private readonly IDatabaseSchemaReaderResolver _readers;
    private readonly ConcurrentDictionary<CacheKey, Lazy<Task<object>>> _entries = new();

    public DatabaseSchemaCache(IDatabaseSchemaReaderResolver readers)
    {
        _readers = readers;
    }

    public async Task<DatabaseSchemaSnapshot> GetSchemaAsync(DatabaseConnectionInfo connection, string? database, CancellationToken cancellationToken = default)
        => (DatabaseSchemaSnapshot)await GetOrLoadAsync(connection, database ?? string.Empty,
            (reader, connectionString) => reader.GetSchemaSnapshotAsync(connectionString, database, CancellationToken.None), cancellationToken);

    public async Task<IReadOnlyList<DatabaseCatalogEntry>> GetDatabasesAsync(DatabaseConnectionInfo connection, CancellationToken cancellationToken = default)
        => (IReadOnlyList<DatabaseCatalogEntry>)await GetOrLoadAsync(connection, DatabasesKey,
            (reader, connectionString) => reader.GetDatabasesAsync(connectionString, CancellationToken.None), cancellationToken);

    public void Invalidate(DatabaseConnectionInfo connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        foreach (var key in _entries.Keys.Where(k => k.ConnectionId == connection.Id))
            _entries.TryRemove(key, out _);
    }

    /// <summary>Cle impossible pour un nom de base : la liste des bases partage le meme dictionnaire.</summary>
    private const string DatabasesKey = "\0databases";

    /// <summary>
    /// Le chargement n'est pas annule avec l'appelant : une frappe qui relance la completion
    /// doit retrouver le meme chargement en cours, pas en redemarrer un.
    /// </summary>
    private async Task<object> GetOrLoadAsync<T>(DatabaseConnectionInfo connection, string scope, Func<IDatabaseSchemaReader, string, Task<T>> load, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(connection);

        var connectionString = connection.ConnectionString
            ?? throw new InvalidOperationException("La connexion n'est pas ouverte.");

        var key = new CacheKey(connection.Id, connectionString, scope.ToUpperInvariant());
        var reader = _readers.Resolve(connection.Provider);

        var entry = _entries.GetOrAdd(key, _ => new Lazy<Task<object>>(async () => (object)await load(reader, connectionString)));

        try
        {
            return await entry.Value.WaitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Un echec (serveur injoignable, droits) ne doit pas rester en cache : on retentera.
            _entries.TryRemove(new KeyValuePair<CacheKey, Lazy<Task<object>>>(key, entry));
            throw;
        }
    }

    /// <summary>La chaine fait partie de la cle : se reconnecter autrement recharge le catalogue.</summary>
    private readonly record struct CacheKey(Guid ConnectionId, string ConnectionString, string Scope);
}
