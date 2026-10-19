using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;
using SqlToolManagerMaui.Databases.Dashboards;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.Globalization;
using System.Windows.Input;

namespace SqlToolManagerMaui.ViewModels.Pages.Documents;

/// <summary>
/// Onglet dashboard (.schx). Le JSON du fichier reste la source : le concepteur ecrit dans
/// la definition puis le texte est reserialise ; la vue Source est relue au retour.
/// </summary>
public partial class DashboardVM : DocumentVM
{
    /// <summary>Lignes lues par carte (tableau ou graphique).</summary>
    private const int CARD_MAX_ROWS = 1000;

    /// <summary>Delai de regroupement des modifications du concepteur avant reecriture du fichier.</summary>
    private static readonly TimeSpan WriteDelay = TimeSpan.FromMilliseconds(250);

    private readonly IDashboardDataService _dashboardData;
    private readonly IConnectionStore _connectionStore;

    private CancellationTokenSource? _cancellation;
    private CancellationTokenSource? _writeDelay;
    private bool _hasPendingWrite;

    /// <summary>Lectures en cours : une nouvelle lecture peut demarrer avant la fin de l'annulation de la precedente.</summary>
    private int _activeLoads;

    public DashboardVM(EditorDocument document,
        DashboardMode mode,
        DatabaseConnectionVM connection,
        StatusBarVM status,
        EditorPreferencesVM preferences,
        ISyntaxConfigurationStore syntaxConfigurations,
        IDashboardDataService dashboardData,
        IConnectionStore connectionStore)
        : base(document, connection, status, preferences)
    {
        _dashboardData = dashboardData;
        _connectionStore = connectionStore;

        Session.DefinitionChanged += OnDefinitionChanged;
        Session.NavigationRequested += OnNavigationRequested;

        Mode = mode;
        Load();

        _ = LoadSqlSyntaxConfigurationAsync(syntaxConfigurations);
    }

    /// <summary>Choix de la liste deroulante des vues : la cle est la vue, la valeur son libelle.</summary>
    public static Dictionary<DashboardMode, string> Modes { get; } = new()
    {
        [DashboardMode.View] = "Dashboard",
        [DashboardMode.Design] = "Concepteur",
        [DashboardMode.Source] = "Source",
    };

    public DashboardSession Session { get; } = new();

    /// <summary>Coloration des requetes des cartes dans le concepteur.</summary>
    [ObservableProperty]
    public partial SyntaxConfiguration? SqlSyntaxConfiguration { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedModeKey), nameof(IsSourceMode), nameof(IsGridMode), nameof(IsDesigning))]
    public partial DashboardMode Mode { get; set; }

    /// <summary>Cle de la liste deroulante (object) : une valeur nulle ou inconnue est ignoree.</summary>
    public object? SelectedModeKey
    {
        get => Mode;
        set
        {
            if (value is DashboardMode mode)
                Mode = mode;
            else
                OnPropertyChanged();
        }
    }

    public bool IsSourceMode => Mode == DashboardMode.Source;

    public bool IsGridMode => Mode != DashboardMode.Source;

    public bool IsDesigning => Mode == DashboardMode.Design;

    /// <summary>Vrai tant qu'une lecture de cartes est en cours (le bouton Arreter n'est actif que pendant).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelLoadingCommand))]
    public partial bool IsLoading { get; private set; }

    public override IAsyncRelayCommand ExecuteCommand => ApplyParametersCommand;

    public override ICommand CancelCommand => CancelLoadingCommand;

    public override string DefaultExtension => DashboardSerializer.FILE_EXTENSION;

    private async Task LoadSqlSyntaxConfigurationAsync(ISyntaxConfigurationStore syntaxConfigurations)
    {
        try
        {
            SqlSyntaxConfiguration = await syntaxConfigurations.GetAsync(SyntaxLanguage.Sql, LifetimeToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>Propose la connexion nommee par le fichier.</summary>
    public override void OnOpened()
    {
        if (Session.Definition?.Connection is { } name)
            Connection.SelectByName(name);

        Status.Set(Session.HasError
            ? $"{Document.FileName} : fichier invalide, corrigez-le dans la vue Source."
            : $"{Document.FileName} ouvert.", isError: Session.HasError);
    }

    /// <summary>Les cartes du dashboard affiche suivent la connexion.</summary>
    protected override void OnConnectionChanged()
    {
        RefreshCanExecute();

        if (IsActive && Session.Current is { } page)
            _ = LoadCardsAsync(page.Cards);
    }

    /// <summary>Relit le fichier s'il a change depuis la derniere lecture ou ecriture, puis ouvre la page de depart.</summary>
    private void Load()
    {
        if (string.Equals(Session.LoadedText, Document.EditorText, StringComparison.Ordinal))
            return;

        DashboardDefinition? definition = null;
        string? error = null;
        try
        {
            definition = DashboardSerializer.Deserialize(Document.EditorText);
        }
        catch (FormatException ex)
        {
            error = ex.Message;
        }

        Session.Load(definition, Document.EditorText, error);
        Session.IsDesigning = IsDesigning;

        if (definition is not null)
            OpenPage(definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    // ---------- Bascule Dashboard / Concepteur / Source ----------

    partial void OnModeChanged(DashboardMode value)
    {
        // La vue Source doit montrer le JSON a jour, et Load comparer au dernier texte ecrit.
        FlushPendingWrites();

        Session.IsDesigning = value == DashboardMode.Design;

        // Retour depuis la vue Source : le JSON a pu etre modifie a la main.
        if (value != DashboardMode.Source)
            Load();
    }

    /// <summary>
    /// Le concepteur a modifie la definition : le fichier est reecrit (l'onglet passe en modifie).
    /// Regroupe : chaque touche frappee dans le concepteur reserialisait tout le JSON.
    /// </summary>
    private void OnDefinitionChanged(object? sender, EventArgs e)
    {
        _hasPendingWrite = true;

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

    public override void PrepareSave() => FlushPendingWrites();

    /// <summary>Reecrit tout de suite le texte si le concepteur l'a modifie.</summary>
    private void FlushPendingWrites()
    {
        CancelWriteDelay();

        if (!_hasPendingWrite || Session.Definition is not { } definition)
            return;

        _hasPendingWrite = false;
        var text = DashboardSerializer.Serialize(definition);
        Session.MarkWritten(text);
        Document.EditorText = text;
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

    private bool CanApplyParameters() => Connection.IsConnected && Session.Current is not null;

    private bool CanCancelLoading() => Connection.IsConnected && IsLoading;

    [RelayCommand(CanExecute = nameof(CanApplyParameters))]
    private Task ApplyParametersAsync()
    {
        if (Session.Current is not { } page)
            return Task.CompletedTask;

        Session.SnapshotValues();
        return LoadCardsAsync(page.Cards);
    }

    /// <summary>Interrompt la lecture des cartes en cours : elles gardent leur dernier resultat.</summary>
    [RelayCommand(CanExecute = nameof(CanCancelLoading))]
    private void CancelLoading() => _cancellation?.Cancel();

    [RelayCommand]
    private void Back() => Session.Back();

    /// <summary>Les etapes du fil d'Ariane gardent leurs resultats : rien n'est relu.</summary>
    [RelayCommand]
    private void GoToStep(DashboardPageState? step)
    {
        if (step is not null)
            Session.GoTo(step);
    }

    /// <summary>Bouton, ou ligne cliquee : les parametres prennent les valeurs prevues et la page cible s'ouvre.</summary>
    private void OnNavigationRequested(object? sender, DashboardNavigationRequest request)
    {
        // Pas de navigation dans le concepteur : un clic y sert a selectionner.
        if (Session.IsDesigning
            || Session.Definition is not { } definition
            || request.Card.Definition.Navigation is not { } navigation)
        {
            return;
        }

        if (definition.FindPage(navigation.Page) is not { } page)
        {
            Status.Set($"La page « {navigation.Page} » n'existe pas dans ce dashboard.", isError: true);
            return;
        }

        Session.SnapshotValues();
        Session.ApplyValues(navigation.Resolve(Session.CurrentValues(), request.Card.Result, request.Row));
        OpenPage(page, asRoot: false);
    }

    private void OpenPage(DashboardPageDefinition page, bool asRoot)
    {
        var state = Session.Open(page, asRoot);

        // Une page est maintenant affichee : Executer peut s'activer.
        RefreshCanExecute();
        _ = LoadCardsAsync(state.Cards);
    }

    // ---------- Lecture des cartes ----------

    /// <summary>Cartes lues en parallele, chacune affichee des qu'elle arrive ; une nouvelle lecture annule la precedente.</summary>
    private async Task LoadCardsAsync(IEnumerable<DashboardCardState> cards)
    {
        var queried = cards.Where(c => c.UsesQuery).ToList();
        if (queried.Count == 0)
            return;

        _activeLoads++;
        IsLoading = true;
        try
        {
            await LoadQueriedCardsAsync(queried);
        }
        finally
        {
            _activeLoads--;
            IsLoading = _activeLoads > 0;
        }
    }

    private async Task LoadQueriedCardsAsync(List<DashboardCardState> queried)
    {
        _cancellation?.Cancel();
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(LifetimeToken);
        var token = _cancellation.Token;

        List<DashboardQueryParameter> parameters = [];
        var isValid = true;
        foreach (var parameter in Session.Parameters)
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

        var database = Session.Definition?.Database;
        await Task.WhenAll(queried.Select(card => LoadCardAsync(connection, database, card, parameters, token)));
    }

    private async Task LoadCardAsync(DatabaseConnectionInfo connection, string? database, DashboardCardState card, IReadOnlyList<DashboardQueryParameter> parameters, CancellationToken token)
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
            // Remplace par une lecture plus recente.
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
        if (IsDesigning)
            Session.SelectedCard = card;
    }

    /// <summary>Nouvelle carte de 4 × 2 cases (reduite au bord droit de la grille).</summary>
    [RelayCommand]
    private void AddCard(DashboardCell? cell)
    {
        if (!IsDesigning || Session.Current is null || cell is null)
            return;

        Session.AddCard(new DashboardCardDefinition
        {
            Title = "Nouvelle carte",
            Column = cell.Column,
            Row = cell.Row,
            ColumnSpan = Math.Min(4, Session.ColumnCount - cell.Column),
            RowSpan = 2,
        });
    }

    [RelayCommand]
    private void StartDrag(DashboardCardState? card)
    {
        if (IsDesigning && card is not null)
        {
            Session.DraggedCard = card;
            Session.SelectedCard = card;
        }
    }

    [RelayCommand]
    private void DropCard(DashboardCell? cell)
    {
        if (!IsDesigning || Session.DraggedCard is not { } card || cell is null)
            return;

        Session.DraggedCard = null;
        card.Column = Math.Clamp(cell.Column, 0, Session.ColumnCount - card.ColumnSpan);
        card.Row = cell.Row;
    }

    [RelayCommand]
    private void MoveCard(string? direction)
    {
        if (Session.SelectedCard is not { } card)
            return;

        switch (direction)
        {
            case "Left":
                card.Column = Math.Max(0, card.Column - 1);
                break;
            case "Right":
                card.Column = Math.Min(Session.ColumnCount - card.ColumnSpan, card.Column + 1);
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
        if (Session.SelectedCard is not { } card)
            return;

        switch (change)
        {
            case "Wider":
                card.ColumnSpan = Math.Min(Session.ColumnCount - card.Column, card.ColumnSpan + 1);
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
            Session.RemoveCard(card);
    }

    [RelayCommand]
    private Task RunCardAsync(DashboardCardState? card)
        => card is not null ? LoadCardsAsync([card]) : Task.CompletedTask;

    // ---------- Concepteur : pages et parametres ----------

    [RelayCommand]
    private void OpenDesignerPage(DashboardPageDefinition? page)
    {
        if (page is not null)
            OpenPage(page, asRoot: true);
    }

    [RelayCommand]
    private void AddPage()
    {
        if (Session.Definition is not { } definition)
            return;

        var number = definition.Pages.Count + 1;
        while (definition.FindPage($"page{number.ToString(CultureInfo.InvariantCulture)}") is not null)
            number++;

        var page = new DashboardPageDefinition { Id = $"page{number.ToString(CultureInfo.InvariantCulture)}", Title = "Nouvelle page" };
        Session.AddPage(page);
        OpenPage(page, asRoot: true);
    }

    /// <summary>Supprime la page affichee (la derniere page restante est conservee).</summary>
    [RelayCommand]
    private void DeletePage()
    {
        if (Session is not { Definition: { } definition, Current: { } current })
            return;

        if (definition.Pages.Count <= 1)
        {
            Status.Set("Un dashboard garde au moins une page.", isError: true);
            return;
        }

        Session.RemovePage(current.Definition);
        OpenPage(definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    [RelayCommand]
    private void AddParameter()
    {
        if (Session.Definition is not { } definition)
            return;

        var number = definition.Parameters.Count + 1;
        while (definition.Parameters.Any(p => string.Equals(p.Name, $"Param{number.ToString(CultureInfo.InvariantCulture)}", StringComparison.OrdinalIgnoreCase)))
            number++;

        Session.AddParameter(new DashboardParameterDefinition { Name = $"Param{number.ToString(CultureInfo.InvariantCulture)}" });
    }

    [RelayCommand]
    private void DeleteParameter(DashboardParameterValue? parameter)
    {
        if (parameter is not null)
            Session.RemoveParameter(parameter);
    }

    public override void Dispose()
    {
        Session.DefinitionChanged -= OnDefinitionChanged;
        Session.NavigationRequested -= OnNavigationRequested;
        CancelWriteDelay();
        _cancellation?.Cancel();
        _cancellation?.Dispose();

        base.Dispose();
    }
}
