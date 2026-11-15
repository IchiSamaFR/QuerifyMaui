using Querify.Core.Services.Editors.Highlighting;
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
using Querify.Diagnostics;

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
        SqlSyntaxConfiguration = await PerfTrace.MeasureAsync("Coloration SQL", () => syntaxConfigurations.GetAsync(SyntaxLanguage.Sql));
        LinqSyntaxConfiguration = await PerfTrace.MeasureAsync("Coloration LINQ", () => syntaxConfigurations.GetAsync(SyntaxLanguage.Linq));
    }

    protected override void OnSessionSwitched(DashboardSession? oldValue, DashboardSession? newValue)
    {
        PerfTrace.MarkMemory($"OnSessionSwitched -> {newValue?.Document.FileName ?? "null"}");
        using var _ = PerfTrace.Measure("OnSessionSwitched (total)");

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
                OpenAsync(newValue).FireAndForget($"Ouverture de {newValue.Document.FileName} en échec.");
        }

        RefreshCanExecute();
    }

    private void OnSessionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DashboardSession.Current) or nameof(DashboardSession.IsLoading))
            RefreshCanExecute();
    }

    private async Task OpenAsync(DashboardSession session)
    {
        // Analyse du JSON hors thread UI : l'onglet s'affiche tout de suite, ses cartes suivent.
        var text = session.Document.EditorText;
        var (definition, error) = await PerfTrace.MeasureAsync($"Open > Deserialize ({text.Length} car.)", () => Task.Run(() => Parse(text)));

        // Onglet quitte entre-temps (il sera ouvert a son retour), ou deja lu par une bascule de vue.
        if (!ReferenceEquals(Session, session) || session.LoadedText is not null)
            return;

        using (PerfTrace.Measure("Open > session.Load"))
            session.Load(definition, text, error);

        // La connexion du fichier est choisie AVANT d'ouvrir la page : sinon les cartes etaient lues
        // une premiere fois sans connexion (ou sur la precedente), puis relues apres le changement.
        // Aucune page n'est ouverte a ce stade : le changement de connexion ne relit rien.
        // Une connexion retenue (et toujours existante) prime sur celle nommee dans le fichier.
        var keepsRemembered = session.HasRememberedConnection && Connection.Exists(session.ConnectionId);
        if (!keepsRemembered && session.Definition?.Connection is { } name)
        {
            using var _ = PerfTrace.Measure($"Open > SelectByName({name})");
            Connection.SelectByName(name);
        }

        OpenStartPage(session);

        if (session.HasError)
            Status.Set($"{session.Document.FileName} : fichier invalide, corrigez-le dans la vue Source.", isError: true);
    }

    /// <summary>Les cartes du dashboard affiche suivent la connexion.</summary>
    protected override void OnConnectionChanged()
    {
        RefreshCanExecute();

        PerfTrace.Mark("OnConnectionChanged -> relecture des cartes");
        if (Session is { Current: { } page } session)
            LoadCardsAsync(session, page.Cards).FireAndForget("Lecture des cartes du dashboard en échec.");
    }

    /// <summary>Relit le fichier s'il a change depuis la derniere lecture ou ecriture (vrai si relu) ; aucune page n'est ouverte.</summary>
    private bool Load(DashboardSession session)
    {
        var text = session.Document.EditorText;
        if (string.Equals(session.LoadedText, text, StringComparison.Ordinal))
            return false;

        var (definition, error) = Parse(text);
        session.Load(definition, text, error);

        return true;
    }

    /// <summary>Definition lue dans le JSON, ou le message d'un fichier illisible.</summary>
    private static (DashboardDefinition? Definition, string? Error) Parse(string text)
    {
        try
        {
            return (DashboardSerializer.Deserialize(text), null);
        }
        catch (Exception ex)
        {
            // Tout fichier illisible s'affiche comme invalide : sinon l'erreur se repetait a chaque bascule d'onglet.
            return (null, ex.Message);
        }
    }

    /// <summary>Ouvre la page de depart du fichier relu (et lit ses cartes).</summary>
    private void OpenStartPage(DashboardSession session)
    {
        if (session.Definition is { } definition)
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
        if (mode != DashboardMode.Source && Load(session))
            OpenStartPage(session);
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
