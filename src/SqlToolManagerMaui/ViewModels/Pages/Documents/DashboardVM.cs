using CodeEdit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Core.Serialization;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;
using SqlToolManagerMaui.Databases.Queries;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.Services.Settings;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.Globalization;

namespace SqlToolManagerMaui.ViewModels.Pages.Documents;

/// <summary>
/// Onglets dashboard (.schx) : un seul VM pour tous, branche par la vue sur la session affichee.
/// Le JSON du fichier reste la source : le concepteur ecrit dans la definition puis le texte est
/// reserialise ; la vue Source est relue au retour.
/// </summary>
[AutoRegister]
public partial class DashboardVM : ObservableObject
{
    /// <summary>Lignes lues par carte (tableau ou graphique).</summary>
    private const int CARD_MAX_ROWS = 1000;

    /// <summary>Delai de regroupement des modifications du concepteur avant reecriture du fichier.</summary>
    private static readonly TimeSpan WriteDelay = TimeSpan.FromMilliseconds(250);

    private readonly IQueryDataService _dashboardData;
    private readonly IConnectionStore _connectionStore;

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

    public DatabaseConnectionVM Connection { get; }

    public StatusBarVM Status { get; }

    public UserPreferences Preferences { get; }

    /// <summary>Session affichee, poussee par DashboardView ; null quand aucun dashboard n'est affiche.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedModeKey))]
    public partial DashboardSession? Session { get; set; }

    /// <summary>Coloration des requetes des cartes dans le concepteur.</summary>
    [ObservableProperty]
    public partial SyntaxConfiguration? SqlSyntaxConfiguration { get; private set; }

    /// <summary>Commandes d'edition du CodeEditor de la vue Source, poussees par le controle (OneWayToSource).</summary>
    [ObservableProperty]
    public partial CodeEditorCommands? EditionCommands { get; set; }

    public DashboardVM(DatabaseConnectionVM connection,
        StatusBarVM status,
        IUserPreferencesStore preferences,
        ISyntaxConfigurationStore syntaxConfigurations,
        IQueryDataService dashboardData,
        IConnectionStore connectionStore)
    {
        Connection = connection;
        Status = status;
        Preferences = preferences.Preferences;
        _dashboardData = dashboardData;
        _connectionStore = connectionStore;

        Connection.SelectedConnectionChanged += (_, _) => OnConnectionChanged();
        _ = LoadSqlSyntaxConfigurationAsync(syntaxConfigurations);
    }

    /// <summary>Cle de la liste deroulante des vues (object) : une valeur nulle ou inconnue est ignoree.</summary>
    public object? SelectedModeKey
    {
        get => Session?.Mode;
        set
        {
            if (value is DashboardMode mode)
                SetMode(mode);
            else
                OnPropertyChanged();
        }
    }

    private async Task LoadSqlSyntaxConfigurationAsync(ISyntaxConfigurationStore syntaxConfigurations)
        => SqlSyntaxConfiguration = await syntaxConfigurations.GetAsync(SyntaxLanguage.Sql);

    partial void OnSessionChanged(DashboardSession? oldValue, DashboardSession? newValue)
    {
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
    private void OnConnectionChanged()
    {
        RefreshCanExecute();

        if (Session is { Current: { } page } session)
            _ = LoadCardsAsync(session, page.Cards);
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

    private void SetMode(DashboardMode mode)
    {
        if (Session is not { } session || session.Mode == mode)
            return;

        // La vue Source doit montrer le JSON a jour, et Load comparer au dernier texte ecrit.
        FlushPendingWrites();
        session.Mode = mode;
        OnPropertyChanged(nameof(SelectedModeKey));

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
        _ = FlushLaterAsync(_writeDelay.Token);
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

    // ---------- Consultation ----------

    private void RefreshCanExecute()
    {
        ApplyParametersCommand.NotifyCanExecuteChanged();
        CancelLoadingCommand.NotifyCanExecuteChanged();
    }

    private bool CanApplyParameters() => Connection.IsConnected && Session?.Current is not null;

    private bool CanCancelLoading() => Connection.IsConnected && Session?.IsLoading == true;

    [RelayCommand(CanExecute = nameof(CanApplyParameters))]
    private Task ApplyParametersAsync()
    {
        if (Session is not { Current: { } page } session)
            return Task.CompletedTask;

        session.SnapshotValues();
        return LoadCardsAsync(session, page.Cards);
    }

    /// <summary>Interrompt la lecture des cartes en cours : elles gardent leur dernier resultat.</summary>
    [RelayCommand(CanExecute = nameof(CanCancelLoading))]
    private void CancelLoading() => Session?.CancelLoad();

    [RelayCommand]
    private void Back() => Session?.Back();

    /// <summary>Les etapes du fil d'Ariane gardent leurs resultats : rien n'est relu.</summary>
    [RelayCommand]
    private void GoToStep(DashboardPageState? step)
    {
        if (step is not null)
            Session?.GoTo(step);
    }

    /// <summary>Bouton, ou ligne cliquee : les parametres prennent les valeurs prevues et la page cible s'ouvre.</summary>
    private void OnNavigationRequested(object? sender, DashboardNavigationRequest request)
    {
        // Pas de navigation dans le concepteur : un clic y sert a selectionner.
        if (sender is not DashboardSession { IsDesigning: false, Definition: { } definition } session
            || request.Card.Definition.Navigation is not { } navigation)
        {
            return;
        }

        if (definition.FindPage(navigation.Page) is not { } page)
        {
            Status.Set($"La page « {navigation.Page} » n'existe pas dans ce dashboard.", isError: true);
            return;
        }

        session.SnapshotValues();
        session.ApplyValues(navigation.Resolve(session.CurrentValues(), request.Card.Result, request.Row));
        OpenPage(session, page, asRoot: false);
    }

    private void OpenPage(DashboardSession session, DashboardPageDefinition page, bool asRoot)
    {
        var state = session.Open(page, asRoot);
        _ = LoadCardsAsync(session, state.Cards);
    }

    // ---------- Lecture des cartes ----------

    /// <summary>Cartes lues en parallele, chacune affichee des qu'elle arrive ; une nouvelle lecture annule la precedente.</summary>
    private async Task LoadCardsAsync(DashboardSession session, IEnumerable<DashboardCardState> cards)
    {
        var queried = cards.Where(c => c.UsesQuery).ToList();
        if (queried.Count == 0)
            return;

        var token = session.BeginLoad();
        try
        {
            await LoadQueriedCardsAsync(session, queried, token);
        }
        finally
        {
            session.EndLoad();
        }
    }

    private async Task LoadQueriedCardsAsync(DashboardSession session, List<DashboardCardState> queried, CancellationToken token)
    {
        List<QueryParameter> parameters = [];
        var isValid = true;
        foreach (var parameter in session.Parameters)
        {
            isValid &= parameter.TryGetQueryParameter(out var value);
            parameters.Add(value);
        }

        if (!isValid)
        {
            Status.Set("Paramètres invalides : corrigez la saisie puis appliquez.", isError: true);
            return;
        }

        DatabaseConnectionInfo connection;
        try
        {
            connection = await OpenConnectionAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (InvalidOperationException ex)
        {
            foreach (var card in queried)
                card.Error = ex.Message;
            Status.Set(ex.Message, isError: true);
            return;
        }

        var database = session.Definition?.Database;
        await Task.WhenAll(queried.Select(card => LoadCardAsync(connection, database, card, parameters, token)));
    }

    private async Task LoadCardAsync(DatabaseConnectionInfo connection, string? database, DashboardCardState card, IReadOnlyList<QueryParameter> parameters, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(card.Definition.Query))
        {
            card.Result = null;
            card.Error = card.IsChart || card.IsValue || card.IsGrid ? "Aucune requête : renseignez-la dans le concepteur." : null;
            return;
        }

        card.IsLoading = true;
        card.Error = null;
        try
        {
            card.Result = await _dashboardData.ReadAsync(connection, database, card.Definition.Query, parameters, CARD_MAX_ROWS, token);
        }
        catch (OperationCanceledException)
        {
            // Remplace par une lecture plus recente, ou onglet ferme.
        }
        catch (Exception ex)
        {
            card.Result = null;
            card.Error = ex.Message;
        }
        finally
        {
            card.IsLoading = false;
        }
    }

    private async Task<DatabaseConnectionInfo> OpenConnectionAsync(CancellationToken token)
    {
        var connection = Connection.ActiveConnection
            ?? throw new InvalidOperationException("Choisissez une connexion pour alimenter les cartes.");

        if (!await _connectionStore.OpenConnectionAsync(connection, token))
            throw new InvalidOperationException($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.");

        return connection;
    }

    // ---------- Concepteur : cartes ----------

    [RelayCommand]
    private void SelectCard(DashboardCardState? card)
    {
        if (Session is { IsDesigning: true } session)
            session.SelectedCard = card;
    }

    /// <summary>Nouvelle carte de 4 × 2 cases (reduite au bord droit de la grille).</summary>
    [RelayCommand]
    private void AddCard(DashboardCell? cell)
    {
        if (Session is not { IsDesigning: true, Current: not null } session || cell is null)
            return;

        session.AddCard(new DashboardCardDefinition
        {
            Title = "Nouvelle carte",
            Column = cell.Column,
            Row = cell.Row,
            ColumnSpan = Math.Min(4, session.ColumnCount - cell.Column),
            RowSpan = 2,
        });
    }

    [RelayCommand]
    private void StartDrag(DashboardCardState? card)
    {
        if (Session is { IsDesigning: true } session && card is not null)
        {
            session.DraggedCard = card;
            session.SelectedCard = card;
        }
    }

    [RelayCommand]
    private void DropCard(DashboardCell? cell)
    {
        if (Session is not { IsDesigning: true, DraggedCard: { } card } session || cell is null)
            return;

        session.DraggedCard = null;
        card.Column = Math.Clamp(cell.Column, 0, session.ColumnCount - card.ColumnSpan);
        card.Row = cell.Row;
    }

    [RelayCommand]
    private void MoveCard(string? direction)
    {
        if (Session is not { SelectedCard: { } card } session)
            return;

        switch (direction)
        {
            case "Left":
                card.Column = Math.Max(0, card.Column - 1);
                break;
            case "Right":
                card.Column = Math.Min(session.ColumnCount - card.ColumnSpan, card.Column + 1);
                break;
            case "Up":
                card.Row = Math.Max(0, card.Row - 1);
                break;
            case "Down":
                card.Row++;
                break;
        }
    }

    [RelayCommand]
    private void ResizeCard(string? change)
    {
        if (Session is not { SelectedCard: { } card } session)
            return;

        switch (change)
        {
            case "Wider":
                card.ColumnSpan = Math.Min(session.ColumnCount - card.Column, card.ColumnSpan + 1);
                break;
            case "Narrower":
                card.ColumnSpan = Math.Max(1, card.ColumnSpan - 1);
                break;
            case "Taller":
                card.RowSpan++;
                break;
            case "Shorter":
                card.RowSpan = Math.Max(1, card.RowSpan - 1);
                break;
        }
    }

    [RelayCommand]
    private void DeleteCard(DashboardCardState? card)
    {
        if (card is not null)
            Session?.RemoveCard(card);
    }

    [RelayCommand]
    private Task RunCardAsync(DashboardCardState? card)
        => Session is { } session && card is not null ? LoadCardsAsync(session, [card]) : Task.CompletedTask;

    // ---------- Concepteur : pages et parametres ----------

    [RelayCommand]
    private void OpenDesignerPage(DashboardPageDefinition? page)
    {
        if (Session is { } session && page is not null)
            OpenPage(session, page, asRoot: true);
    }

    [RelayCommand]
    private void AddPage()
    {
        if (Session is not { Definition: { } definition } session)
            return;

        var number = definition.Pages.Count + 1;
        while (definition.FindPage($"page{number.ToString(CultureInfo.InvariantCulture)}") is not null)
            number++;

        var page = new DashboardPageDefinition { Id = $"page{number.ToString(CultureInfo.InvariantCulture)}", Title = "Nouvelle page" };
        session.AddPage(page);
        OpenPage(session, page, asRoot: true);
    }

    /// <summary>Supprime la page affichee (la derniere page restante est conservee).</summary>
    [RelayCommand]
    private void DeletePage()
    {
        if (Session is not { Definition: { } definition, Current: { } current } session)
            return;

        if (definition.Pages.Count <= 1)
        {
            Status.Set("Un dashboard garde au moins une page.", isError: true);
            return;
        }

        session.RemovePage(current.Definition);
        OpenPage(session, definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    [RelayCommand]
    private void AddParameter()
    {
        if (Session is not { Definition: { } definition } session)
            return;

        var number = definition.Parameters.Count + 1;
        while (definition.Parameters.Any(p => string.Equals(p.Name, $"Param{number.ToString(CultureInfo.InvariantCulture)}", StringComparison.OrdinalIgnoreCase)))
            number++;

        session.AddParameter(new DashboardParameterDefinition { Name = $"Param{number.ToString(CultureInfo.InvariantCulture)}" });
    }

    [RelayCommand]
    private void DeleteParameter(DashboardParameterValue? parameter)
    {
        if (parameter is not null)
            Session?.RemoveParameter(parameter);
    }

    [RelayCommand]
    private void ShowSettings()
    {
        Preferences.Panels.IsDashboardParamsOpen = !Preferences.Panels.IsDashboardParamsOpen;
    }
}
