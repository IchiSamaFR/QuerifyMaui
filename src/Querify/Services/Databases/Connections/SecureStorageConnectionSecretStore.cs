using Querify.Core.Ioc;
using Querify.Databases.Connections;

namespace Querify.Services.Databases.Connections;

/// <summary>Chaines de connexion chiffrees par le SecureStorage de la plateforme, sous la cle <c>connectionstring:{id}</c>.</summary>
[AutoRegister(As = typeof(IConnectionSecretStore))]
public sealed class SecureStorageConnectionSecretStore : IConnectionSecretStore
{
    private const string KeyPrefix = "connectionstring:";

    /// <inheritdoc/>
    public Task<string?> GetAsync(Guid id) => SecureStorage.Default.GetAsync(Key(id));

    /// <inheritdoc/>
    public Task SetAsync(Guid id, string connectionString) => SecureStorage.Default.SetAsync(Key(id), connectionString);

    /// <inheritdoc/>
    public void Remove(Guid id) => SecureStorage.Default.Remove(Key(id));

    private static string Key(Guid id) => $"{KeyPrefix}{id:N}";
}
