using SqlToolManagerMaui.Models;

namespace SqlToolManagerMaui.Services;

/// <summary>
/// Persistance des connexions : descripteurs en JSON, chaines de connexion en SecureStorage.
/// </summary>
public interface IConnectionStore
{
    /// <summary>Descripteurs enregistres, tries par nom.</summary>
    Task<IReadOnlyList<DatabaseConnectionInfo>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Chaine de connexion complete, ou null si elle n'est plus dans le coffre.</summary>
    Task<string?> GetConnectionStringAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Cree ou met a jour une connexion (le descripteur est identifie par son Id).</summary>
    Task SaveAsync(DatabaseConnectionInfo info, string connectionString, CancellationToken cancellationToken = default);

    /// <summary>Supprime le descripteur et la chaine associee.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
