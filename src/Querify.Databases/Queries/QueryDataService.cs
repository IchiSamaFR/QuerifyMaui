using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;

namespace Querify.Databases.Queries;

/// <inheritdoc cref="IQueryDataService"/>
public sealed class QueryDataService : IQueryDataService
{
    private readonly IQueryReaderResolver _readers;

    public QueryDataService(IQueryReaderResolver readers)
    {
        _readers = readers;
    }

    public Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var connectionString = connection.ConnectionString
            ?? throw new InvalidOperationException($"La connexion {connection.Name} n'est pas ouverte.");

        return _readers.Resolve(connection.Provider).ReadAsync(connectionString, database, query, parameters, maxRows, cancellationToken);
    }
}
