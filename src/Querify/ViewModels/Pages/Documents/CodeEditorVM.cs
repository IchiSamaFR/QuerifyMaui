using Querify.Core.Services.Editors.Highlighting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors;
using Querify.Core.Models.Editors.Highlighting;
using Querify.Databases.Connections;
using Querify.Languages.Queries;
using Querify.Services.Editors;
using Querify.Services.Editors.Highlighting;
using Querify.Services.Settings;
using Querify.UI.Extensions;
using Querify.ViewModels.Pages.Shared;

namespace Querify.ViewModels.Pages.Documents;

/// <summary>
/// Onglets de script (SQL, LINQ...) : un seul VM pour tous, branche par la vue sur la session affichee.
/// Edition dans le CodeEditor, execution de la selection ou de tout le document sur la connexion active ;
/// le resultat est range dans la session d'origine.
/// </summary>
[AutoRegister]
public partial class CodeEditorVM : DocumentVM<CodeEditorSession>
{
    private readonly ISyntaxConfigurationStore _syntaxConfigurations;
    private readonly IConnectionStore _connectionStore;
    private readonly IQueryExecutorResolver _executors;

    /// <summary>Configurations deja lues : changer d'onglet ne relit aucun .xshd.</summary>
    private readonly Dictionary<SyntaxLanguage, SyntaxConfiguration?> _syntaxByLanguage = [];

    /// <summary>Coloration, commentaires et autocompletion du langage de la session.</summary>
    [ObservableProperty]
    public partial SyntaxConfiguration? SyntaxConfiguration { get; private set; }

    public CodeEditorVM(DatabaseConnectionVM connection,
        StatusBarVM status,
        IUserPreferencesStore preferences,
        ISyntaxConfigurationStore syntaxConfigurations,
        IConnectionStore connectionStore,
        IQueryExecutorResolver executors)
        : base(connection, status, preferences)
    {
        _syntaxConfigurations = syntaxConfigurations;
        _connectionStore = connectionStore;
        _executors = executors;
    }

    protected override void OnSessionSwitched(CodeEditorSession? oldValue, CodeEditorSession? newValue)
    {
        ExecuteQueryCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedLanguageKey));
        RefreshSyntaxConfigurationAsync().FireAndForget("Coloration syntaxique indisponible pour cet onglet.");
    }

    protected override void RefreshCanExecute() => ExecuteQueryCommand.NotifyCanExecuteChanged();

    private async Task RefreshSyntaxConfigurationAsync()
    {
        if (Session is not { } session)
            return;

        var language = session.Document.Language;
        if (!_syntaxByLanguage.TryGetValue(language, out var configuration))
        {
            configuration = await _syntaxConfigurations.GetAsync(language);
            _syntaxByLanguage[language] = configuration;
        }

        // Onglet ou langage change pendant la lecture : la configuration lue n'est plus la bonne.
        if (Session == session && session.Document.Language == language)
            SyntaxConfiguration = configuration;
    }

    // ---------- Langage ----------

    /// <summary>Cle de la liste deroulante du langage (object) : une valeur nulle ou inconnue est ignoree.</summary>
    public object? SelectedLanguageKey
    {
        get => Session?.Document.Language;
        set
        {
            if (value is SyntaxLanguage language)
                SetLanguageAsync(language).FireAndForget("Changement de langage impossible.");
            else
                OnPropertyChanged();
        }
    }

    /// <summary>Change le langage de l'onglet ; le texte n'est pas touche.</summary>
    private async Task SetLanguageAsync(SyntaxLanguage language)
    {
        if (Session is not { } session || session.Document.Language == language)
            return;

        session.Document.Language = language;
        await RefreshSyntaxConfigurationAsync();
        Status.Set($"Langage de l'onglet : {SyntaxConfiguration?.Definition.Name ?? language.ToString()}.");
    }

    // ---------- Execution ----------

    private bool CanExecuteQuery() => Connection.IsConnected && Session is not null;

    /// <summary>Volontairement hors RunAsBusyAsync : la page doit rester active pour pouvoir annuler.</summary>
    [RelayCommand(CanExecute = nameof(CanExecuteQuery), IncludeCancelCommand = true)]
    private async Task ExecuteQueryAsync(CancellationToken token)
    {
        // Session capturee : l'utilisateur peut changer d'onglet pendant l'execution.
        var session = Session;
        var connection = Connection.ActiveConnection;
        if (session is null || connection is null)
            return;

        var document = session.Document;
        var selectedText = document.Selection.SelectedText;
        var isSelection = !string.IsNullOrWhiteSpace(selectedText);
        var script = isSelection ? selectedText : document.EditorText;
        if (string.IsNullOrWhiteSpace(script))
        {
            Status.Set("Rien à exécuter : le document est vide.", isError: true);
            return;
        }

        session.LastResult = null;
        session.LiveMessages = [];
        session.IsExecuting = true;
        Status.Set(isSelection
            ? $"Exécution de la sélection sur {connection.Name}..."
            : $"Exécution sur {connection.Name}...");

        // Fermer l'onglet arrete aussi l'execution.
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, session.LifetimeToken);

        // Cree sur le thread UI : le script ecrit depuis le sien, l'affichage suit par lots.
        var liveMessages = new LiveQueryMessages(Dispatcher.GetForCurrentThread()!, messages => session.LiveMessages = messages);
        try
        {
            // Ouverture a la demande : la connexion est partagee avec l'explorateur.
            if (!await _connectionStore.OpenConnectionAsync(connection, cancellation.Token))
            {
                Status.Set($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.", isError: true);
                return;
            }

            // LINQ : la premiere execution sur une base genere et compile son DbContext (quelques secondes).
            var result = await _executors.Resolve(document.Language).ExecuteAsync(connection, script, liveMessages, cancellation.Token);
            liveMessages.Close();
            session.LastResult = result;
            session.LiveMessages = [];
            Status.Set(result.StatusLabel, isError: result.HasErrors || result.WasCancelled);
        }
        catch (OperationCanceledException)
        {
            Status.Set("Exécution annulée.", isError: true);
        }
        catch (Exception ex)
        {
            Status.Set($"Échec de l'exécution : {ex.Message}", isError: true);
        }
        finally
        {
            liveMessages.Close();
            session.IsExecuting = false;
        }
    }

    /// <summary>Masque le panneau de resultats de la session affichee.</summary>
    [RelayCommand]
    private void CloseResults()
    {
        if (Session is { IsExecuting: false } session)
            session.LastResult = null;
    }
}
