using Querify.Core.Models.Dashboards;
using Querify.Core.Models.Editors;
using Querify.Core.Models.Settings;

namespace Querify.ViewModels.Pages;

/// <summary>
/// Partie "connexion memorisee" : la connexion de chaque fichier est retenue (par chemin) et reprise a sa
/// reouverture si elle existe encore ; la selectionner ouvre la connexion.
/// </summary>
public partial class MainVM
{
    private const int MaxFileConnections = 200;

    /// <summary>Applique a un onglet de fichier la connexion retenue pour ce fichier, si elle existe encore.</summary>
    private void ApplyRememberedConnection(AbstractSession session, Guid? connectionId = null)
    {
        connectionId ??= RememberedConnection(session.Document.FilePath);
        if (connectionId is null)
            return;

        // Connexion supprimee depuis : comportement habituel (connexion de l'onglet courant). Liste pas encore
        // chargee : l'identifiant est garde, l'onglet la retrouvera a l'arrivee de la liste.
        if (_connection.Connections.Count > 0 && !_connection.Exists(connectionId))
            return;

        session.ConnectionId = connectionId;

        // Le choix de l'utilisateur l'emporte sur la connexion nommee dans le .schx.
        if (session is DashboardSession dashboard)
            dashboard.HasRememberedConnection = true;
    }

    private Guid? RememberedConnection(string? path)
        => path is null ? null : Preferences.FileConnections.FirstOrDefault(c => IsSamePath(c.Path, path))?.ConnectionId;

    /// <summary>Retient la connexion de l'onglet pour son fichier (aucune connexion : l'entree est oubliee).</summary>
    private void RememberConnection(AbstractSession? session)
    {
        if (session?.Document.FilePath is not { } path)
            return;

        var current = RememberedConnection(path);
        if (current == session.ConnectionId)
            return;

        var others = Preferences.FileConnections.Where(c => !IsSamePath(c.Path, path));
        Preferences.FileConnections = session.ConnectionId is { } id
            ? [new FileConnection { Path = path, ConnectionId = id }, .. others.Take(MaxFileConnections - 1)]
            : [.. others];
    }

    /// <summary>Fichier renomme ou deplace : sa connexion le suit.</summary>
    private void MoveRememberedConnection(string oldPath, string newPath)
    {
        if (RememberedConnection(oldPath) is not { } id)
            return;

        Preferences.FileConnections =
        [
            new FileConnection { Path = newPath, ConnectionId = id },
            .. Preferences.FileConnections.Where(c => !IsSamePath(c.Path, oldPath) && !IsSamePath(c.Path, newPath)),
        ];
    }
}
