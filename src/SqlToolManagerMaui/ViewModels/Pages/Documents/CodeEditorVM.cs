using CodeEdit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Languages.Queries;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.Services.Settings;
using SqlToolManagerMaui.ViewModels.Pages.Shared;

namespace SqlToolManagerMaui.ViewModels.Pages.Documents;

/// <summary>
/// Onglets de script (SQL, LINQ...) : un seul VM pour tous, branche par la vue sur la session affichee.
/// Edition dans le CodeEditor, execution de la selection ou de tout le document sur la connexion active ;
/// le resultat est range dans la session d'origine.
/// </summary>
[AutoRegister]
public partial class CodeEditorVM : ObservableObject
{
    private readonly ISyntaxConfigurationStore _syntaxConfigurations;
    private readonly IConnectionStore _connectionStore;
    private readonly IQueryExecutorResolver _executors;

    /// <summary>Configurations deja lues : changer d'onglet ne relit aucun .xshd.</summary>
    private readonly Dictionary<SyntaxLanguage, SyntaxConfiguration?> _syntaxByLanguage = [];

    public DatabaseConnectionVM Connection { get; }

    public StatusBarVM Status { get; }

    public UserPreferences Preferences { get; }

    /// <summary>Session affichee, poussee par CodeEditorView ; null quand aucun onglet de script n'est affiche.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetLanguageCommand))]
    public partial CodeEditorSession? Session { get; set; }

    /// <summary>Coloration, commentaires et autocompletion du langage de la session.</summary>
    [ObservableProperty]
    public partial SyntaxConfiguration? SyntaxConfiguration { get; private set; }

    /// <summary>Commandes d'edition du CodeEditor, poussees par le controle (OneWayToSource).</summary>
    [ObservableProperty]
    public partial CodeEditorCommands? EditionCommands { get; set; }

    public CodeEditorVM(DatabaseConnectionVM connection,
        StatusBarVM status,
        IUserPreferencesStore preferences,
        ISyntaxConfigurationStore syntaxConfigurations,
        IConnectionStore connectionStore,
        IQueryExecutorResolver executors)
    {
        Connection = connection;
        Status = status;
        Preferences = preferences.Preferences;
        _syntaxConfigurations = syntaxConfigurations;
        _connectionStore = connectionStore;
        _executors = executors;

        Connection.SelectedConnectionChanged += (_, _) => ExecuteQueryCommand.NotifyCanExecuteChanged();
    }

    partial void OnSessionChanged(CodeEditorSession? value) => _ = RefreshSyntaxConfigurationAsync();

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

    private bool HasSession() => Session is not null;

    /// <summary>Change le langage de l'onglet ; le texte n'est pas touche.</summary>
    [RelayCommand(CanExecute = nameof(HasSession))]
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
        session.IsExecuting = true;
        Status.Set(isSelection
            ? $"Exécution de la sélection sur {connection.Name}..."
            : $"Exécution sur {connection.Name}...");

        // Fermer l'onglet arrete aussi l'execution.
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token, session.LifetimeToken);
        try
        {
            // Ouverture a la demande : la connexion est partagee avec l'explorateur.
            if (!await _connectionStore.OpenConnectionAsync(connection, cancellation.Token))
            {
                Status.Set($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.", isError: true);
                return;
            }

            // LINQ : la premiere execution sur une base genere et compile son DbContext (quelques secondes).
            var result = await _executors.Resolve(document.Language).ExecuteAsync(connection, script, cancellation.Token);
            session.LastResult = result;
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
