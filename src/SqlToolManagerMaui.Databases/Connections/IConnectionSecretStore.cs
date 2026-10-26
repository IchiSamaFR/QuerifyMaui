namespace SqlToolManagerMaui.Databases.Connections;

/// <summary>Coffre chiffre des chaines de connexion completes, fourni par l'hote (SecureStorage dans l'application).</summary>
public interface IConnectionSecretStore
{
    /// <summary>Chaine enregistree pour la connexion, null si absente ; peut lever si le coffre est illisible.</summary>
    Task<string?> GetAsync(Guid id);

    Task SetAsync(Guid id, string connectionString);

    void Remove(Guid id);
}
