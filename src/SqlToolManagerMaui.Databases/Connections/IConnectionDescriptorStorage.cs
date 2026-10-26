using SqlToolManagerMaui.Databases.Models.Connections;

namespace SqlToolManagerMaui.Databases.Connections;

/// <summary>Persistance des descripteurs de connexion (sans chaine de connexion), fournie par l'hote.</summary>
public interface IConnectionDescriptorStorage
{
    /// <summary>
    /// Descripteurs enregistres ; liste vide si rien n'est enregistre (ou fichier corrompu mis de cote).
    /// Null si la source est illisible : rien n'est alors reecrit, pour ne pas effacer les connexions.
    /// </summary>
    Task<List<DatabaseConnectionInfo>?> ReadAsync(CancellationToken cancellationToken = default);

    /// <summary>Reecrit la liste complete.</summary>
    Task WriteAsync(IReadOnlyList<DatabaseConnectionInfo> items, CancellationToken cancellationToken = default);
}
