using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Services.Editors;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.UI.ViewModels;
using SqlToolManagerMaui.ViewModels.Pages.Queries;
using System.Collections.ObjectModel;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>
/// Editeur de texte multi-onglets (facon Notepad) pose sur CodeEdit.Maui.
/// Chaque onglet est un <see cref="EditorDocument"/> ; la page n'affiche qu'un seul
/// <c>CodeEditor</c>, dont la propriete Document suit <see cref="ActiveDocument"/>.
/// Les autres responsabilites sont deleguees aux sous-VM de <c>Queries/</c>.
/// </summary>
[AutoRegister]
public partial class QueryVM : AbstractVM, IQueryWorkspace
{
    private readonly IEditorFileService _files;
    private readonly ISyntaxConfigurationStore _syntaxConfigurations;

    /// <summary>Compteur des documents "Sans titre" ouverts depuis le demarrage.</summary>
    private int _untitledCount;

    /// <summary>Onglets ouverts, dans l'ordre d'ouverture.</summary>
    public ObservableCollection<EditorDocument> Documents { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SyntaxConfiguration))]
    public partial EditorDocument? ActiveDocument { get; set; }

    /// <summary>Barre d'etat (enregistrement, erreur d'ouverture...).</summary>
    public StatusBarVM Status { get; }

    /// <summary>Connexion choisie dans le Picker.</summary>
    public QueryConnectionVM Connection { get; }

    /// <summary>Execution de la selection ou du document, et panneau de resultats.</summary>
    public QueryExecutionVM Execution { get; }

    /// <summary>Onglets .schx : consultation et concepteur.</summary>
    public DashboardVM Dashboard { get; }

    /// <summary>Fichiers recents, explorateur et options d'edition.</summary>
    public EditorPreferencesVM Preferences { get; }

    CancellationToken IQueryWorkspace.LifetimeToken => LifetimeToken;

    /// <summary>Configurations de tous les langages, chargees une fois : changer d'onglet ne relit aucun .xshd.</summary>
    private readonly Dictionary<SyntaxLanguage, SyntaxConfiguration> _syntaxByLanguage = [];

    /// <summary>Langage de l'onglet actif (coloration, commentaires, autocompletion).</summary>
    public SyntaxConfiguration? SyntaxConfiguration
        => ActiveDocument is { } document && _syntaxByLanguage.TryGetValue(document.Language, out var configuration) ? configuration : null;

    /// <summary>Coloration SQL, quel que soit l'onglet actif (requetes des cartes du dashboard).</summary>
    public SyntaxConfiguration? SqlSyntaxConfiguration => _syntaxByLanguage.GetValueOrDefault(SyntaxLanguage.Sql);

    public QueryVM(IEditorFileService files, ISyntaxConfigurationStore syntaxConfigurations, StatusBarVM status, QueryConnectionVM connection, QueryExecutionVM execution, DashboardVM dashboard, EditorPreferencesVM preferences)
    {
        _files = files;
        _syntaxConfigurations = syntaxConfigurations;

        Status = status;
        Connection = connection;
        Execution = execution;
        Dashboard = dashboard;
        Preferences = preferences;

        Execution.Attach(this);
        Dashboard.Attach(this);
        Preferences.Attach(this);
        Connection.SelectedConnectionChanged += OnSelectedConnectionChanged;
        // En dernier : l'abonnement au store peut deja rappeler la page.
        Connection.Attach(this);

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
            OnPropertyChanged(nameof(SqlSyntaxConfiguration));
        }

        await Preferences.LoadAsync(LifetimeToken);
        await Connection.RefreshAsync(LifetimeToken);

        if (Documents.Count == 0)
            New();
    }

    /// <summary>Ferme l'application (entree Fichier > Quitter).</summary>
    [RelayCommand]
    private static void Quit() => Application.Current?.Quit();

    partial void OnActiveDocumentChanged(EditorDocument? value)
    {
        Execution.RefreshCanExecute();
        Dashboard.RefreshCanExecute();
    }

    private void OnSelectedConnectionChanged(object? sender, EventArgs e)
    {
        Execution.RefreshCanExecute();
        Dashboard.RefreshCanExecute();
        Dashboard.OnConnectionChanged();
    }

    // ---------- Insertion depuis l'explorateur ----------

    /// <summary>Colle la requete au curseur (a la place de la selection) et bascule sur sa connexion.</summary>
    private void InsertQuery(InsertQueryMessage message)
    {
        // Un dashboard n'accueille pas de SQL : la requete part dans un nouvel onglet.
        if (ActiveDocument is null or { IsDashboard: true })
            New();

        if (ActiveDocument is not { } document)
            return;

        Connection.Select(message.Connection.Id);

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
}
