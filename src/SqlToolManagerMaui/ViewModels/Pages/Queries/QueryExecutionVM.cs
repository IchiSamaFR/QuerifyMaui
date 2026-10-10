using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Languages.Queries;
using SqlToolManagerMaui.Services.Databases;

namespace SqlToolManagerMaui.ViewModels.Pages.Queries;

/// <summary>
/// Execution de la page Requete : la selection, ou tout le document a defaut, sur la connexion active.
/// Le resultat est range sur l'onglet d'origine.
/// </summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class QueryExecutionVM : ObservableObject
{
    private readonly IConnectionStore _connectionStore;
    private readonly IQueryExecutorResolver _executors;

    // Fourni par Attach : QueryVM recoit ce VM par injection avant de pouvoir se passer en hote.
    private IQueryWorkspace _workspace = null!;

    public QueryExecutionVM(IConnectionStore connectionStore, IQueryExecutorResolver executors)
    {
        _connectionStore = connectionStore;
        _executors = executors;
    }

    /// <summary>Branche le VM sur la page hote ; appele une seule fois, par QueryVM.</summary>
    public void Attach(IQueryWorkspace workspace) => _workspace = workspace;

    /// <summary>A appeler quand l'onglet actif ou la connexion change.</summary>
    public void RefreshCanExecute() => ExecuteQueryCommand.NotifyCanExecuteChanged();

    private bool CanExecuteQuery() => _workspace.Connection.IsConnected && _workspace.ActiveDocument is { IsDashboard: false };

    /// <summary>Volontairement hors RunAsBusyAsync : la page doit rester active pour pouvoir annuler.</summary>
    [RelayCommand(CanExecute = nameof(CanExecuteQuery), IncludeCancelCommand = true)]
    private async Task ExecuteQueryAsync(CancellationToken token)
    {
        var document = _workspace.ActiveDocument;
        var connection = _workspace.Connection.ActiveConnection;
        if (document is null || connection is null)
            return;

        var status = _workspace.Status;
        var selectedText = document.Selection.SelectedText;
        var isSelection = !string.IsNullOrWhiteSpace(selectedText);
        var script = isSelection ? selectedText : document.EditorText;
        if (string.IsNullOrWhiteSpace(script))
        {
            status.Set("Rien à exécuter : le document est vide.", isError: true);
            return;
        }

        document.LastResult = null;
        document.IsExecuting = true;
        status.Set(isSelection
            ? $"Exécution de la sélection sur {connection.Name}..."
            : $"Exécution sur {connection.Name}...");

        try
        {
            // Ouverture a la demande : la connexion est partagee avec l'explorateur.
            if (!await _connectionStore.OpenConnectionAsync(connection, token))
            {
                status.Set($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.", isError: true);
                return;
            }

            // LINQ : la premiere execution sur une base genere et compile son DbContext (quelques secondes).
            var result = await _executors.Resolve(document.Language).ExecuteAsync(connection, script, token);
            document.LastResult = result;
            status.Set(result.StatusLabel, isError: result.HasErrors || result.WasCancelled);
        }
        catch (OperationCanceledException)
        {
            status.Set("Exécution annulée.", isError: true);
        }
        catch (Exception ex)
        {
            status.Set($"Échec de l'exécution : {ex.Message}", isError: true);
        }
        finally
        {
            document.IsExecuting = false;
        }
    }

    /// <summary>Masque le panneau de resultats de l'onglet courant.</summary>
    [RelayCommand]
    private void CloseResults()
    {
        if (_workspace.ActiveDocument is { IsExecuting: false } document)
            document.LastResult = null;
    }
}
