using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace SqlToolManagerMaui.Core.Models.Databases.Connections;

/// <summary>
/// Descripteur d'une connexion enregistree.
/// ATTENTION : cet objet est serialise en clair dans connections.json.
/// La chaine de connexion complete n'y figure jamais : elle vit dans le SecureStorage
/// (voir <see cref="Services.Databases.Connections.ConnectionStore"/>), seules les metadonnees d'affichage
/// (serveur, base) sont recopiees ici.
/// <see cref="ConnectionString"/> n'est renseignee qu'en memoire, quand la connexion est
/// ouverte (<see cref="IsConnected"/>) ; l'attribut Newtonsoft [JsonIgnore] l'exclut du fichier
/// (celui de System.Text.Json serait ignore par Newtonsoft).
/// </summary>
public sealed partial class DatabaseConnectionInfo : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    public partial Guid Id { get; set; } = Guid.Empty;

    /// <summary>Libelle affiche dans la liste.</summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

    /// <summary>Serveur extrait de la chaine, pour l'affichage uniquement.</summary>
    [ObservableProperty]
    public partial string Server { get; set; } = string.Empty;

    /// <summary>Base extraite de la chaine, pour l'affichage uniquement.</summary>
    [ObservableProperty]
    public partial string Database { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>Chaine complete, en memoire uniquement : renseignee = connecte, null = deconnecte.</summary>
    [JsonIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    public partial string? ConnectionString { get; set; }

    [JsonIgnore]
    public bool IsConnected => !string.IsNullOrWhiteSpace(ConnectionString);

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

    public bool IsNew => Id == Guid.Empty;
    public bool IsEditing => !IsNew;

    public bool CloseConnection()
    {
        if (IsConnected)
        {
            ConnectionString = null;
            return true;
        }
        return false;
    }
}
