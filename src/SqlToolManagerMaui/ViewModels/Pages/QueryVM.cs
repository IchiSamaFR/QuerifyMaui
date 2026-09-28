using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Models.Databases;
using SqlToolManagerMaui.Models.Editors;
using SqlToolManagerMaui.Models.Editors.Highlighting;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.Services.Queries;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>
/// Editeur de texte multi-onglets (facon Notepad) pose sur CodeEdit.Maui.
/// Chaque onglet est un <see cref="EditorDocument"/> ; la page n'affiche qu'un seul
/// <c>CodeEditor</c>, dont la propriete Document suit <see cref="ActiveDocument"/>.
/// L'execution porte sur le texte selectionne, ou sur tout le document a defaut, et son
/// resultat est range sur l'onglet d'origine.
/// </summary>
public partial class QueryVM : AbstractVM
{
    private readonly IEditorFileService _files;
    private readonly IConnectionStore _connectionStore;
    private readonly ISyntaxConfigurationStore _syntaxConfigurations;
    private readonly IQueryExecutorResolver _executors;

    /// <summary>Compteur des documents "Sans titre" ouverts depuis le demarrage.</summary>
    private int _untitledCount;

    /// <summary>Onglets ouverts, dans l'ordre d'ouverture.</summary>
    public ObservableCollection<EditorDocument> Documents { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteQueryCommand))]
    public partial EditorDocument? ActiveDocument { get; set; }

    /// <summary>
    /// Entree sentinelle placee en tete de <see cref="Connections"/> : la selectionner
    /// equivaut a se deconnecter. Son Id vide la distingue d'une vraie connexion.
    /// </summary>
    public static DatabaseConnectionInfo NoConnection { get; } = new()
    {
        Id = Guid.Empty,
        Name = "Aucune connexion",
    };

    /// <summary>
    /// Connexions proposees pour l'execution. Le premier element est toujours
    /// <see cref="NoConnection"/>, pour que le Picker permette de se deconnecter.
    /// </summary>
    [ObservableProperty]
    public partial List<DatabaseConnectionInfo> Connections { get; set; } = [NoConnection];

    /// <summary>Element selectionne dans le Picker, jamais null : <see cref="NoConnection"/> par defaut.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveConnection))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteQueryCommand))]
    public partial DatabaseConnectionInfo? SelectedConnection { get; set; } = NoConnection;

    /// <summary>Connexion reellement retenue, ou null si l'utilisateur est deconnecte.</summary>
    public DatabaseConnectionInfo? ActiveConnection
        => SelectedConnection is null || SelectedConnection.Id == Guid.Empty ? null : SelectedConnection;

    public bool IsConnected => ActiveConnection is not null;

    /// <summary>Message affiche dans la barre d'etat (enregistrement, erreur d'ouverture...).</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsStatusError { get; set; }

    // ---------- Options d'edition, liees directement au CodeEditor ----------

    public EditorConfiguration EditorConfiguration { get; } = new()
    {
        IsLineDisplayed = true,
        IsWordWrapped = false,
        IsTabIndentation = false,
        IndentationSize = 4
    };

    // ---------- Barre d'etat ----------

    /// <summary>Curseur et texte selectionne, alimentes par le CodeEditor.</summary>
    public EditorSelection Selection { get; } = new();

    /// <summary>Langage colore par l'editeur : pour l'instant toujours du T-SQL.</summary>
    [ObservableProperty]
    public partial SyntaxConfiguration? SyntaxConfiguration { get; set; }

    public QueryVM(IEditorFileService files, IConnectionStore connectionStore, ISyntaxConfigurationStore syntaxConfigurations, IQueryExecutorResolver executors)
    {
        _files = files;
        _connectionStore = connectionStore;
        _syntaxConfigurations = syntaxConfigurations;
        _executors = executors;
    }

    protected override async Task LoadDatasBusyAsync()
    {
        SyntaxConfiguration ??= await _syntaxConfigurations.GetAsync(SyntaxLanguage.Sql, LifetimeToken);

        var stored = await _connectionStore.GetAllAsync(LifetimeToken);

        // La sentinelle "Aucune connexion" reste en tete : c'est l'option de deconnexion.
        Connections = [NoConnection, .. stored];

        // Une connexion supprimee entre-temps ne doit pas rester selectionnee.
        if (ActiveConnection is not null && !stored.Any(c => c.Id == ActiveConnection.Id))
            SelectedConnection = NoConnection;

        if (Documents.Count == 0)
            New();
    }

    /// <summary>Ferme l'application (entree Fichier > Quitter).</summary>
    [RelayCommand]
    private static void Quit() => Application.Current?.Quit();

    // ---------- Connexion ----------

    /// <summary>Repasse le Picker sur "Aucune connexion".</summary>
    [RelayCommand(CanExecute = nameof(IsConnected))]
    private void Disconnect()
    {
        SelectedConnection = NoConnection;
    }

    partial void OnSelectedConnectionChanged(DatabaseConnectionInfo? value)
    {
        SetStatus(ActiveConnection is null
            ? "Deconnecte."
            : $"Connexion active : {ActiveConnection.Name}.");
    }

    // ---------- Execution ----------

    private bool CanExecuteQuery() => IsConnected && ActiveDocument is not null;

    /// <summary>
    /// Execute la selection, ou tout le document si rien n'est selectionne.
    /// Volontairement hors RunAsBusyAsync : la page doit rester active pour pouvoir annuler.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteQuery), IncludeCancelCommand = true)]
    private async Task ExecuteQueryAsync(CancellationToken token)
    {
        var document = ActiveDocument;
        var connection = ActiveConnection;
        if (document is null || connection is null)
            return;

        var isSelection = !string.IsNullOrWhiteSpace(Selection.SelectedText);
        var script = isSelection ? Selection.SelectedText : document.EditorText;
        if (string.IsNullOrWhiteSpace(script))
        {
            SetStatus("Rien à exécuter : le document est vide.", isError: true);
            return;
        }

        document.LastResult = null;
        document.IsExecuting = true;
        SetStatus(isSelection
            ? $"Exécution de la sélection sur {connection.Name}..."
            : $"Exécution sur {connection.Name}...");

        try
        {
            var connectionString = await _connectionStore.GetConnectionStringAsync(connection.Id, token);
            if (connectionString is null)
            {
                SetStatus($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.", isError: true);
                return;
            }

            var result = await _executors.Resolve(connection.Provider).ExecuteAsync(connectionString, script, token);
            document.LastResult = result;
            SetStatus(result.StatusLabel, isError: result.HasErrors || result.WasCancelled);
        }
        catch (OperationCanceledException)
        {
            SetStatus("Exécution annulée.", isError: true);
        }
        catch (Exception ex)
        {
            SetStatus($"Échec de l'exécution : {ex.Message}", isError: true);
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
        if (ActiveDocument is { IsExecuting: false } document)
            document.LastResult = null;
    }

    public void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
