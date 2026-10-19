using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Languages.Queries;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.Windows.Input;

namespace SqlToolManagerMaui.ViewModels.Pages.Documents;

/// <summary>
/// Onglet de script (SQL, LINQ...) : edition dans le CodeEditor, execution de la selection
/// ou de tout le document sur la connexion active, et resultats propres a l'onglet.
/// </summary>
public partial class CodeEditorVM : DocumentVM
{
    private readonly ISyntaxConfigurationStore _syntaxConfigurations;
    private readonly IConnectionStore _connectionStore;
    private readonly IQueryExecutorResolver _executors;

    public CodeEditorVM(EditorDocument document,
        DatabaseConnectionVM connection,
        StatusBarVM status,
        EditorPreferencesVM preferences,
        ISyntaxConfigurationStore syntaxConfigurations,
        IConnectionStore connectionStore,
        IQueryExecutorResolver executors)
        : base(document, connection, status, preferences)
    {
        _syntaxConfigurations = syntaxConfigurations;
        _connectionStore = connectionStore;
        _executors = executors;

        _ = LoadSyntaxConfigurationAsync();
    }

    /// <summary>Coloration, commentaires et autocompletion du langage de l'onglet.</summary>
    [ObservableProperty]
    public partial SyntaxConfiguration? SyntaxConfiguration { get; private set; }

    public SyntaxLanguage Language => Document.Language;

    /// <summary>Resultat de la derniere execution lancee depuis cet onglet.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResultsPanel))]
    public partial QueryExecutionResult? LastResult { get; set; }

    /// <summary>Vrai tant qu'une execution lancee depuis cet onglet est en cours.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResultsPanel))]
    public partial bool IsExecuting { get; set; }

    /// <summary>Le panneau de resultats n'apparait qu'apres une premiere execution.</summary>
    public bool HasResultsPanel => IsExecuting || LastResult is not null;

    public override IAsyncRelayCommand ExecuteCommand => ExecuteQueryCommand;

    public override ICommand CancelCommand => ExecuteQueryCancelCommand;

    /// <summary>Premiere extension declaree par le .xshd du langage.</summary>
    public override string DefaultExtension
        => SyntaxConfiguration is { Definition.Extensions: [var extension, ..] } ? extension : ".sql";

    private async Task LoadSyntaxConfigurationAsync()
    {
        var language = Document.Language;
        SyntaxConfiguration? configuration;
        try
        {
            configuration = await _syntaxConfigurations.GetAsync(language, LifetimeToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Langage change pendant le chargement : la configuration lue n'est plus la bonne.
        if (Document.Language == language)
            SyntaxConfiguration = configuration;
    }

    protected override void OnConnectionChanged() => ExecuteQueryCommand.NotifyCanExecuteChanged();

    // ---------- Langage ----------

    /// <summary>Change le langage de l'onglet ; le texte n'est pas touche.</summary>
    [RelayCommand]
    private async Task SetLanguageAsync(SyntaxLanguage language)
    {
        if (Document.Language == language)
            return;

        Document.Language = language;
        OnPropertyChanged(nameof(Language));

        await LoadSyntaxConfigurationAsync();
        Status.Set($"Langage de l'onglet : {SyntaxConfiguration?.Definition.Name ?? language.ToString()}.");
    }

    // ---------- Execution ----------

    private bool CanExecuteQuery() => Connection.IsConnected;

    /// <summary>Volontairement hors RunAsBusyAsync : la page doit rester active pour pouvoir annuler.</summary>
    [RelayCommand(CanExecute = nameof(CanExecuteQuery), IncludeCancelCommand = true)]
    private async Task ExecuteQueryAsync(CancellationToken token)
    {
        var connection = Connection.ActiveConnection;
        if (connection is null)
            return;

        var selectedText = Document.Selection.SelectedText;
        var isSelection = !string.IsNullOrWhiteSpace(selectedText);
        var script = isSelection ? selectedText : Document.EditorText;
        if (string.IsNullOrWhiteSpace(script))
        {
            Status.Set("Rien à exécuter : le document est vide.", isError: true);
            return;
        }

        LastResult = null;
        IsExecuting = true;
        Status.Set(isSelection
            ? $"Exécution de la sélection sur {connection.Name}..."
            : $"Exécution sur {connection.Name}...");

        try
        {
            // Ouverture a la demande : la connexion est partagee avec l'explorateur.
            if (!await _connectionStore.OpenConnectionAsync(connection, token))
            {
                Status.Set($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.", isError: true);
                return;
            }

            // LINQ : la premiere execution sur une base genere et compile son DbContext (quelques secondes).
            var result = await _executors.Resolve(Document.Language).ExecuteAsync(connection, script, token);
            LastResult = result;
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
            IsExecuting = false;
        }
    }

    /// <summary>Masque le panneau de resultats.</summary>
    [RelayCommand]
    private void CloseResults()
    {
        if (!IsExecuting)
            LastResult = null;
    }

    // ---------- Insertion depuis l'explorateur ----------

    /// <summary>Colle la requete au curseur (a la place de la selection) et bascule sur sa connexion.</summary>
    public void InsertQuery(InsertQueryMessage message)
    {
        Connection.Select(message.Connection.Id);

        var text = Document.EditorText ?? string.Empty;
        var newLine = text.Contains("\r\n") ? "\r\n" : text.Contains('\r') ? "\r" : Environment.NewLine;

        var selection = Document.Selection;
        var start = Math.Clamp(Math.Min(selection.Anchor, selection.Active), 0, text.Length);
        var end = Math.Clamp(Math.Max(selection.Anchor, selection.Active), 0, text.Length);

        // Curseur en milieu de ligne : la requete part sur une nouvelle ligne.
        var prefix = start > 0 && text[start - 1] is not ('\n' or '\r') ? newLine : string.Empty;
        var query = prefix + message.Query.Replace("\n", newLine) + newLine;

        // Selection d'abord : l'editeur la restaure apres le changement de texte (curseur apres la requete).
        selection.Anchor = selection.Active = start + query.Length;
        Document.EditorText = text[..start] + query + text[end..];
    }

    public override void Dispose()
    {
        if (ExecuteQueryCommand.IsRunning)
            ExecuteQueryCommand.Cancel();

        base.Dispose();
    }
}
