using System.Text.Json.Serialization;

namespace SqlToolManagerMaui.Models;

/// <summary>
/// Descripteur d'une connexion enregistree.
/// ATTENTION : cet objet est serialise en clair dans connections.json.
/// La chaine de connexion complete n'y figure jamais : elle vit dans le SecureStorage
/// (voir <see cref="Services.ConnectionStore"/>), seules les metadonnees d'affichage
/// (serveur, base) sont recopiees ici.
/// </summary>
public sealed class DatabaseConnectionInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Libelle affiche dans la liste.</summary>
    public string Name { get; set; } = string.Empty;

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

    /// <summary>Serveur extrait de la chaine, pour l'affichage uniquement.</summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>Base extraite de la chaine, pour l'affichage uniquement.</summary>
    public string Database { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>Sous-titre de la liste : "serveur / base", ou l'un des deux.</summary>
    [JsonIgnore]
    public string Subtitle => (Server, Database) switch
    {
        ("", "") => "Chaine de connexion enregistree",
        (var s, "") => s,
        ("", var d) => d,
        var (s, d) => $"{s} / {d}",
    };

    [JsonIgnore]
    public string ProviderLabel => Provider switch
    {
        DatabaseProvider.SqlServer => "SQL Server",
        _ => Provider.ToString(),
    };

    public DatabaseConnectionInfo Clone() => (DatabaseConnectionInfo)MemberwiseClone();
}
