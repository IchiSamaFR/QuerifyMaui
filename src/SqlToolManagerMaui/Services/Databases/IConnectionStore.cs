
using SqlToolManagerMaui.Databases.Models.Connections;

namespace SqlToolManagerMaui.Services.Databases;

/// <summary>
/// Persistance des connexions : descripteurs en JSON, chaines de connexion en SecureStorage.
/// </summary>
public interface IConnectionStore
{
    /// <summary>Recupere la chaine de connexion associee a l'Id, ou null si elle est absente.</summary>
    Task<string?> GetConnectionStringAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Descripteurs enregistres, tries par nom. Les instances sont partagees : un changement est vu par tous.</summary>
    Task<bool> RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>Descripteurs enregistres, tries par nom. Les instances sont partagees : un changement est vu par tous.</summary>
    Task<bool> Subscribe(Action<IReadOnlyList<DatabaseConnectionInfo>> action, CancellationToken cancellationToken = default);

    /// <summary>Descripteur enregistres, tries par nom. Les instances sont partagees : un changement est vu par tous.</summary>
    Task<bool> Unsubscribe(Action<IReadOnlyList<DatabaseConnectionInfo>> action);

    /// <summary>
    /// Ouvre la connexion : lit la chaine dans le coffre et la place dans
    /// <see cref="DatabaseConnectionInfo.ConnectionString"/>. Faux si elle est introuvable.
    /// </summary>
    Task<bool> OpenConnectionAsync(DatabaseConnectionInfo info, CancellationToken cancellationToken = default);

    /// <summary>Ferme la connexion (vide la chaine en memoire, le coffre est inchange). Faux si deja fermee.</summary>
    bool CloseConnection(DatabaseConnectionInfo info);

    /// <summary>Cree ou met a jour une connexion (le descripteur est identifie par son Id).</summary>
    Task SaveAsync(DatabaseConnectionInfo info, string connectionString, CancellationToken cancellationToken = default);

    /// <summary>Supprime le descripteur et la chaine associee.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
