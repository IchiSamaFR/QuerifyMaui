using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.Services.Linq;
using SqlToolManagerMaui.Services.Queries;
using SqlToolManagerMaui.UI.ViewModels;
using System.Collections.ObjectModel;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>
/// Editeur de texte multi-onglets (facon Notepad) pose sur CodeEdit.Maui.
/// Chaque onglet est un <see cref="EditorDocument"/> ; la page n'affiche qu'un seul
/// <c>CodeEditor</c>, dont la propriete Document suit <see cref="ActiveDocument"/>.
/// L'execution porte sur le texte selectionne, ou sur tout le document a defaut, et son
/// resultat est range sur l'onglet d'origine.
/// </summary>
[AutoRegister]
public partial class QueryVM : AbstractVM
{
    private readonly IEditorFileService _files;
    private readonly IConnectionStore _connectionStore;
    private readonly ISyntaxConfigurationStore _syntaxConfigurations;
    private readonly IQueryExecutorResolver _executors;
    private readonly ILinqQueryExecutor _linqExecutor;

    /// <summary>Compteur des documents "Sans titre" ouverts depuis le demarrage.</summary>
    private int _untitledCount;

    /// <summary>Onglets ouverts, dans l'ordre d'ouverture.</summary>
    public ObservableCollection<EditorDocument> Documents { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteQueryCommand))]
    [NotifyPropertyChangedFor(nameof(SyntaxConfiguration))]
    public partial EditorDocument? ActiveDocument { get; set; }

    /// <summary>Connexions proposees pour l'execution : les instances partagees du store.</summary>
    [ObservableProperty]
    public partial List<DatabaseConnectionInfo> Connections { get; set; } = [];

    /// <summary>Element selectionne dans le Picker ; null = aucune connexion (Title du Picker).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveConnection))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteQueryCommand))]
    public partial DatabaseConnectionInfo? SelectedConnection { get; set; }

    /// <summary>Connexion reellement retenue, ou null si l'utilisateur est deconnecte.</summary>
    public DatabaseConnectionInfo? ActiveConnection => SelectedConnection;

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

    /// <summary>Configurations de tous les langages, chargees une fois : changer d'onglet ne relit aucun .xshd.</summary>
    private readonly Dictionary<SyntaxLanguage, SyntaxConfiguration> _syntaxByLanguage = [];

    /// <summary>Langage de l'onglet actif (coloration, commentaires, autocompletion).</summary>
    public SyntaxConfiguration? SyntaxConfiguration
        => ActiveDocument is { } document && _syntaxByLanguage.TryGetValue(document.Language, out var configuration) ? configuration : null;

    public QueryVM(IEditorFileService files, IConnectionStore connectionStore, ISyntaxConfigurationStore syntaxConfigurations, IQueryExecutorResolver executors, ILinqQueryExecutor linqExecutor)
    {
        _files = files;
        _connectionStore = connectionStore;
        _connectionStore.Subscribe(ApplyConnections);
        _syntaxConfigurations = syntaxConfigurations;
        _executors = executors;
        _linqExecutor = linqExecutor;

        // Ajout, modification ou suppression depuis l'explorateur : le Picker suit le coffre.
        WeakReferenceMessenger.Default.Register<QueryVM, InsertQueryMessage>(this, static (vm, message) => vm.InsertQuery(message));
    }

    protected override async Task LoadDatasBusyAsync()
    {
        if (_syntaxByLanguage.Count == 0)
        {
            foreach (var language in Enum.GetValues<SyntaxLanguage>())
            {
                if (await _syntaxConfigurations.GetAsync(language, LifetimeToken) is { } configuration)
                    _syntaxByLanguage[language] = configuration;
            }

            OnPropertyChanged(nameof(SyntaxConfiguration));
        }

        await ReloadConnectionsAsync();

        if (Documents.Count == 0)
            New();
    }

    private async Task ReloadConnectionsAsync()
    {
        await _connectionStore.RefreshAsync(LifetimeToken);
    }

    /// <summary>Ferme l'application (entree Fichier > Quitter).</summary>
    [RelayCommand]
    private static void Quit() => Application.Current?.Quit();

    // ---------- Connexion ----------

    private void ApplyConnections(IReadOnlyList<DatabaseConnectionInfo> connections)
    {
        var selectedConnectionId = SelectedConnection?.Id;

        Connections = connections.ToList();

        if (selectedConnectionId is not null)
            SelectedConnection = Connections.FirstOrDefault(c => c.Id == selectedConnectionId) ?? SelectedConnection;
    }

    /// <summary>Reinitialise la selection du Picker (aucune connexion).</summary>
    [RelayCommand(CanExecute = nameof(IsConnected))]
    private void Disconnect()
    {
        SelectedConnection = null;
    }

    partial void OnSelectedConnectionChanged(DatabaseConnectionInfo? value)
    {
        SetStatus(ActiveConnection is null
            ? "Deconnecte."
            : $"Connexion active : {ActiveConnection.Name}.");

        if (ActiveConnection != null)
            _connectionStore.OpenConnectionAsync(ActiveConnection);
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

        var selectedText = document.Selection.SelectedText;
        var isSelection = !string.IsNullOrWhiteSpace(selectedText);
        var script = isSelection ? selectedText : document.EditorText;
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
            // Ouverture a la demande : la connexion est partagee avec l'explorateur.
            if (!await _connectionStore.OpenConnectionAsync(connection, token))
            {
                SetStatus($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.", isError: true);
                return;
            }

            // LINQ : la premiere execution sur une base genere et compile son DbContext (quelques secondes).
            var result = document.Language == SyntaxLanguage.Linq
                ? await _linqExecutor.ExecuteAsync(connection, script, token)
                : await _executors.Resolve(connection.Provider).ExecuteAsync(connection.ConnectionString!, script, token);
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

    // ---------- Insertion depuis l'explorateur ----------

    /// <summary>Colle la requete au curseur (a la place de la selection) et bascule sur sa connexion.</summary>
    private void InsertQuery(InsertQueryMessage message)
    {
        if (ActiveDocument is null)
            New();

        if (ActiveDocument is not { } document)
            return;

        SelectedConnection = Connections.FirstOrDefault(c => c.Id == message.Connection.Id) ?? SelectedConnection;

        var text = document.EditorText ?? string.Empty;
        var newLine = text.Contains("\r\n") ? "\r\n" : text.Contains('\r') ? "\r" : Environment.NewLine;

        var selection = document.Selection;
        var start = Math.Clamp(Math.Min(selection.Anchor, selection.Active), 0, text.Length);
        var end = Math.Clamp(Math.Max(selection.Anchor, selection.Active), 0, text.Length);

        // Curseur en milieu de ligne : la requete part sur une nouvelle ligne.
        var prefix = start > 0 && text[start - 1] is not ('\n' or '\r') ? newLine : string.Empty;
        var query = prefix + message.Query.Replace("\n", newLine) + newLine;

        // Selection d'abord : l'editeur la restaure apres le changement de texte (curseur apres la requete).
        selection.Anchor = selection.Active = start + query.Length;
        document.EditorText = text[..start] + query + text[end..];
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
