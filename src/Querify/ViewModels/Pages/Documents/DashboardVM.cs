using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Querify.Core.Enums.Dashboards;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Dashboards;
using Querify.Core.Models.Editors.Highlighting;
using Querify.Core.Serialization;
using Querify.Databases.Connections;
using Querify.Languages.Queries;
using Querify.Services.Dashboards;
using Querify.Services.Dialogs;
using Querify.Services.Editors.Highlighting;
using Querify.Services.Settings;
using Querify.UI.Extensions;
using Querify.ViewModels.Pages.Shared;

namespace Querify.ViewModels.Pages.Documents;

/// <summary>
/// Onglets dashboard (.schx) : un seul VM pour tous, branche par la vue sur la session affichee.
/// Le JSON du fichier reste la source : le concepteur ecrit dans la definition puis le texte est
/// reserialise ; la vue Source est relue au retour.
/// </summary>
[AutoRegister]
public partial class DashboardVM : DocumentVM<DashboardSession>
{
    /// <summary>Lignes lues par carte (tableau ou graphique).</summary>
    private const int CARD_MAX_ROWS = 1000;

    /// <summary>Delai de regroupement des modifications du concepteur avant reecriture du fichier.</summary>
    private static readonly TimeSpan WriteDelay = TimeSpan.FromMilliseconds(250);

    private readonly IQueryExecutorResolver _executors;
    private readonly IConnectionStore _connectionStore;
    private readonly IDashboardParameterDialogService _parameterDialogs;
    private readonly IDialogService _dialogs;

    /// <summary>Sessions dont la definition a change depuis la derniere reecriture du texte.</summary>
    private readonly HashSet<DashboardSession> _pendingWrites = [];
    private CancellationTokenSource? _writeDelay;

    /// <summary>Choix de la liste deroulante des vues : la cle est la vue, la valeur son libelle.</summary>
    public static Dictionary<DashboardMode, string> Modes { get; } = new()
    {
        [DashboardMode.View] = "Dashboard",
        [DashboardMode.Design] = "Concepteur",
        [DashboardMode.Source] = "Source",
    };

    /// <summary>Coloration des requetes des cartes dans le concepteur.</summary>
    [ObservableProperty]
    public partial SyntaxConfiguration? SqlSyntaxConfiguration { get; private set; }

    /// <summary>Coloration et autocompletion des cartes LINQ (EF).</summary>
    [ObservableProperty]
    public partial SyntaxConfiguration? LinqSyntaxConfiguration { get; private set; }

    public DashboardVM(DatabaseConnectionVM connection,
        StatusBarVM status,
        IUserPreferencesStore preferences,
        ISyntaxConfigurationStore syntaxConfigurations,
        IQueryExecutorResolver executors,
        IConnectionStore connectionStore,
        IDashboardParameterDialogService parameterDialogs,
        IDialogService dialogs)
        : base(connection, status, preferences)
    {
        _executors = executors;
        _connectionStore = connectionStore;
        _parameterDialogs = parameterDialogs;
        _dialogs = dialogs;

        LoadSyntaxConfigurationsAsync(syntaxConfigurations).FireAndForget("Coloration des requêtes du concepteur indisponible.");
    }

    /// <summary>Bascule Dashboard / Concepteur de l'en-tete ; depuis la vue Source, l'activer ouvre le concepteur.</summary>
    public bool IsDesignMode
    {
        get => Session?.Mode == DashboardMode.Design;
        set
        {
            SetMode(value ? DashboardMode.Design : DashboardMode.View);

            // Sans session (ou mode inchange), le controle doit revenir sur l'etat reel.
            OnPropertyChanged();
        }
    }

    private async Task LoadSyntaxConfigurationsAsync(ISyntaxConfigurationStore syntaxConfigurations)
    {
        SqlSyntaxConfiguration = await syntaxConfigurations.GetAsync(SyntaxLanguage.Sql);
        LinqSyntaxConfiguration = await syntaxConfigurations.GetAsync(SyntaxLanguage.Linq);
    }

    protected override void OnSessionSwitched(DashboardSession? oldValue, DashboardSession? newValue)
    {
        OnPropertyChanged(nameof(IsDesignMode));

        // Les modifications de l'onglet quitte sont ecrites tout de suite.
        FlushPendingWrites();

        if (oldValue is not null)
        {
            oldValue.DefinitionChanged -= OnDefinitionChanged;
            oldValue.NavigationRequested -= OnNavigationRequested;
            oldValue.PropertyChanged -= OnSessionPropertyChanged;
        }

        if (newValue is not null)
        {
            newValue.DefinitionChanged += OnDefinitionChanged;
            newValue.NavigationRequested += OnNavigationRequested;
            newValue.PropertyChanged += OnSessionPropertyChanged;

            // Premier affichage : lecture du fichier et connexion qu'il nomme.
            if (newValue.LoadedText is null)
                Open(newValue);
        }

        RefreshCanExecute();
    }

    private void OnSessionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DashboardSession.Current) or nameof(DashboardSession.IsLoading))
            RefreshCanExecute();
    }

    private void Open(DashboardSession session)
    {
        Load(session);

        if (session.Definition?.Connection is { } name)
            Connection.SelectByName(name);

        if (session.HasError)
            Status.Set($"{session.Document.FileName} : fichier invalide, corrigez-le dans la vue Source.", isError: true);
    }

    /// <summary>Les cartes du dashboard affiche suivent la connexion.</summary>
    protected override void OnConnectionChanged()
    {
        RefreshCanExecute();

        if (Session is { Current: { } page } session)
            LoadCardsAsync(session, page.Cards).FireAndForget("Lecture des cartes du dashboard en échec.");
    }

    /// <summary>Relit le fichier s'il a change depuis la derniere lecture ou ecriture, puis ouvre la page de depart.</summary>
    private void Load(DashboardSession session)
    {
        var text = session.Document.EditorText;
        if (string.Equals(session.LoadedText, text, StringComparison.Ordinal))
            return;

        DashboardDefinition? definition = null;
        string? error = null;
        try
        {
            definition = DashboardSerializer.Deserialize(text);
        }
        catch (FormatException ex)
        {
            error = ex.Message;
        }

        session.Load(definition, text, error);

        if (definition is not null)
            OpenPage(session, definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    // ---------- Bascule Dashboard / Concepteur / Source ----------

    [RelayCommand]
    private void SetMode(DashboardMode mode)
    {
        if (Session is not { } session || session.Mode == mode)
            return;

        // La vue Source doit montrer le JSON a jour, et Load comparer au dernier texte ecrit.
        FlushPendingWrites();
        session.Mode = mode;
        OnPropertyChanged(nameof(IsDesignMode));

        // Retour depuis la vue Source : le JSON a pu etre modifie a la main.
        if (mode != DashboardMode.Source)
            Load(session);
    }

    /// <summary>
    /// Le concepteur a modifie la definition : le fichier est reecrit (l'onglet passe en modifie).
    /// Regroupe : chaque touche frappee dans le concepteur reserialisait tout le JSON.
    /// </summary>
    private void OnDefinitionChanged(object? sender, EventArgs e)
    {
        if (sender is not DashboardSession session)
            return;

        _pendingWrites.Add(session);

        CancelWriteDelay();
        _writeDelay = new CancellationTokenSource();
        FlushLaterAsync(_writeDelay.Token).FireAndForget("Dashboard non réécrit dans le texte du fichier.");
    }

    private async Task FlushLaterAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(WriteDelay, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        FlushPendingWrites();
    }

    /// <summary>Reecrit tout de suite le texte des sessions modifiees ; appele avant un enregistrement ou un changement de vue.</summary>
    public void FlushPendingWrites()
    {
        CancelWriteDelay();

        foreach (var session in _pendingWrites)
        {
            if (session.Definition is not { } definition)
                continue;

            var text = DashboardSerializer.Serialize(definition);
            session.MarkWritten(text);
            session.Document.EditorText = text;
        }

        _pendingWrites.Clear();
    }

    private void CancelWriteDelay()
    {
        _writeDelay?.Cancel();
        _writeDelay?.Dispose();
        _writeDelay = null;
    }
}
