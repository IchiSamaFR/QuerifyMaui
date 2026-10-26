using Querify.Core.Models.Settings;

namespace Querify.Services.Settings;

/// <summary>Persistance des onglets ouverts (workspace.json dans AppDataDirectory).</summary>
public interface IWorkspaceStore
{
    /// <summary>Onglets memorises, ou null s'il n'y en a pas (ou si le fichier est illisible).</summary>
    Task<WorkspaceState?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Synchrone : appele pendant la fermeture de la fenetre, qui n'attend pas une tache.</summary>
    void Save(WorkspaceState state);

    void Clear();
}
