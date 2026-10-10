using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Editors;
using System.Collections.ObjectModel;

namespace SqlToolManagerMaui.ViewModels.Pages.Queries;

/// <summary>Etat de la page Requete partage avec ses sous-VM (implemente par QueryVM).</summary>
public interface IQueryWorkspace
{
    ObservableCollection<EditorDocument> Documents { get; }

    EditorDocument? ActiveDocument { get; set; }

    IReadOnlyList<DatabaseConnectionInfo> Connections { get; }

    DatabaseConnectionInfo? SelectedConnection { get; set; }

    StatusBarVM Status { get; }

    /// <summary>Annule au dechargement de la page.</summary>
    CancellationToken LifetimeToken { get; }

    /// <summary>Tache suivie par l'indicateur d'occupation de la page (fourni par AbstractVM).</summary>
    Task RunAsBusyAsync(Func<CancellationToken, Task> taskFunc, bool blocking = true, bool skipIfBusy = false);

    /// <inheritdoc cref="RunAsBusyAsync(Func{CancellationToken, Task}, bool, bool)"/>
    void StartAsBusy(Func<CancellationToken, Task> taskFunc, bool blocking = true, bool skipIfBusy = false);
}
