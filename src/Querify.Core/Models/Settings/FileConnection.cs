namespace Querify.Core.Models.Settings;

/// <summary>Derniere connexion choisie pour un fichier (requete ou dashboard), reprise a sa reouverture.</summary>
public sealed class FileConnection
{
    public string Path { get; set; } = string.Empty;

    public Guid ConnectionId { get; set; }
}
