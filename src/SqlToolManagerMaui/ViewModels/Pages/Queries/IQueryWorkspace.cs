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
}
