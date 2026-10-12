using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Enums.Editors;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Databases.Dashboards;
using SqlToolManagerMaui.Services.Databases;
using System.Collections.Specialized;
using System.Globalization;

namespace SqlToolManagerMaui.ViewModels.Pages.Queries;

/// <summary>
/// Onglets dashboard (.schx) de la page Requete. Le JSON du fichier reste la source : le concepteur
/// ecrit dans la definition puis le texte est reserialise ; la vue Source est relue au retour.
/// </summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class DashboardVM : ObservableObject
{
    /// <summary>Lignes lues par carte (tableau ou graphique).</summary>
    private const int CARD_MAX_ROWS = 1000;

    private readonly IDashboardDataService _dashboardData;
    private readonly IConnectionStore _connectionStore;

    // Fourni par Attach : QueryVM recoit ce VM par injection avant de pouvoir se passer en hote.
    private IQueryWorkspace _workspace = null!;

    /// <summary>Delai de regroupement des modifications du concepteur avant reecriture du fichier.</summary>
    private static readonly TimeSpan WriteDelay = TimeSpan.FromMilliseconds(250);

    private CancellationTokenSource? _cancellation;
    private int _untitledCount;

    /// <summary>Onglets dont la definition a change depuis la derniere reecriture du texte.</summary>
    private readonly HashSet<EditorDocument> _pendingWrites = [];
    private CancellationTokenSource? _writeDelay;

    public DashboardVM(IDashboardDataService dashboardData, IConnectionStore connectionStore)
    {
        _dashboardData = dashboardData;
        _connectionStore = connectionStore;
    }

    private EditorDocument? ActiveDocument => _workspace.ActiveDocument;

    private StatusBarVM Status => _workspace.Status;

    /// <summary>Branche le VM sur la page hote ; appele une seule fois, par QueryVM.</summary>
    public void Attach(IQueryWorkspace workspace)
    {
        _workspace = workspace;
        workspace.Documents.CollectionChanged += OnDocumentsCollectionChanged;
    }

    // ---------- Onglets ----------

    private void OnDocumentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var document in e.OldItems?.OfType<EditorDocument>().Where(d => d.IsDashboard) ?? [])
        {
            _pendingWrites.Remove(document);
            document.Dashboard.DefinitionChanged -= OnDefinitionChanged;
            document.Dashboard.NavigationRequested -= OnNavigationRequested;
        }

        foreach (var document in e.NewItems?.OfType<EditorDocument>().Where(d => d.IsDashboard) ?? [])
        {
            document.Dashboard.DefinitionChanged += OnDefinitionChanged;
            document.Dashboard.NavigationRequested += OnNavigationRequested;
        }
    }

    private EditorDocument? FindDocument(object? session)
        => _workspace.Documents.FirstOrDefault(d => ReferenceEquals(d.Dashboard, session));

    /// <summary>Nouveau dashboard : une page vide, ouverte dans le concepteur, sur la connexion active.</summary>
    [RelayCommand]
    private void New()
    {
        _untitledCount++;
        var definition = DashboardDefinition.CreateEmpty(_workspace.Connection.ActiveConnection?.Name, database: null);

        // Texte « enregistre » vide : l'onglet reste modifie tant qu'il n'a pas ete enregistre.
        var document = new EditorDocument($"Dashboard {_untitledCount.ToString(CultureInfo.InvariantCulture)}", filePath: null, fileText: string.Empty, SyntaxLanguage.None, DocumentKind.Dashboard)
        {
            EditorText = DashboardSerializer.Serialize(definition),
            DashboardMode = DashboardMode.Design,
        };

        Open(document);
    }

    /// <summary>Ajoute l'onglet, lit le fichier, propose la connexion qu'il nomme et ouvre la premiere page.</summary>
    public void Open(EditorDocument document)
    {
        _workspace.Documents.Add(document);
        _workspace.ActiveDocument = document;

        Load(document, force: true);

        if (document.Dashboard.Definition?.Connection is { } name)
            _workspace.Connection.SelectByName(name);

        Status.Set(document.Dashboard.HasError
            ? $"{document.FileName} : fichier invalide, corrigez-le dans la vue Source."
            : $"{document.FileName} ouvert.", isError: document.Dashboard.HasError);
    }

    /// <summary>Les cartes du dashboard affiche suivent la connexion.</summary>
    public void OnConnectionChanged()
    {
        if (ActiveDocument is { IsDashboard: true, Dashboard.Current: { } page } document)
            _ = LoadCardsAsync(document, page.Cards);
    }

    /// <summary>Relit le fichier s'il a change depuis la derniere lecture ou ecriture, puis ouvre la page de depart.</summary>
    private void Load(EditorDocument document, bool force = false)
    {
        var session = document.Dashboard;
        if (!force && string.Equals(session.LoadedText, document.EditorText, StringComparison.Ordinal))
            return;

        DashboardDefinition? definition = null;
        string? error = null;
        try
        {
            definition = DashboardSerializer.Deserialize(document.EditorText);
        }
        catch (FormatException ex)
        {
            error = ex.Message;
        }

        session.Load(definition, document.EditorText, error);
        session.IsDesigning = document.DashboardMode == DashboardMode.Design;

        if (definition is not null)
            OpenPage(document, definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    // ---------- Bascule Dashboard / Concepteur / Source ----------

    [RelayCommand]
    private void ShowView() => SetMode(DashboardMode.View);

    [RelayCommand]
    private void ShowDesigner() => SetMode(DashboardMode.Design);

    [RelayCommand]
    private void ShowSource() => SetMode(DashboardMode.Source);

    private void SetMode(DashboardMode mode)
    {
        if (ActiveDocument is not { IsDashboard: true } document || document.DashboardMode == mode)
            return;

        // La vue Source doit montrer le JSON a jour, et Load comparer au dernier texte ecrit.
        FlushPendingWrites();

        document.DashboardMode = mode;
        document.Dashboard.IsDesigning = mode == DashboardMode.Design;

        // Retour depuis la vue Source : le JSON a pu etre modifie a la main.
        if (mode != DashboardMode.Source)
            Load(document);
    }

    /// <summary>
    /// Le concepteur a modifie la definition : le fichier est reecrit (l'onglet passe en modifie).
    /// Regroupe : chaque touche frappee dans le concepteur reserialisait tout le JSON et rechargeait
    /// l'editeur texte (masque) de l'onglet, coloration comprise.
    /// </summary>
    private void OnDefinitionChanged(object? sender, EventArgs e)
    {
        if (FindDocument(sender) is not { } document)
            return;

        _pendingWrites.Add(document);

        _writeDelay?.Cancel();
        _writeDelay?.Dispose();
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

    /// <summary>Reecrit tout de suite le texte des onglets modifies ; appele avant un enregistrement ou un changement de vue.</summary>
    public void FlushPendingWrites()
    {
        _writeDelay?.Cancel();
        _writeDelay?.Dispose();
        _writeDelay = null;

        foreach (var document in _pendingWrites)
        {
            if (document.Dashboard.Definition is not { } definition)
                continue;

            var text = DashboardSerializer.Serialize(definition);
            document.Dashboard.MarkWritten(text);
            document.EditorText = text;
        }

        _pendingWrites.Clear();
    }

    // ---------- Consultation ----------

    /// <summary>Vrai tant qu'une lecture de cartes est en cours (le bouton Arreter n'est actif que pendant).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelLoadingCommand))]
    public partial bool IsLoading { get; private set; }

    /// <summary>Lectures en cours : une nouvelle lecture peut demarrer avant la fin de l'annulation de la precedente.</summary>
    private int _activeLoads;

    /// <summary>A rappeler quand la connexion ou l'onglet actif change.</summary>
    public void RefreshCanExecute()
    {
        ApplyParametersCommand.NotifyCanExecuteChanged();
        CancelLoadingCommand.NotifyCanExecuteChanged();
    }

    private bool CanApplyParameters()
        => _workspace.Connection.IsConnected && ActiveDocument is { IsDashboard: true, Dashboard.Current: not null };

    private bool CanCancelLoading() => _workspace.Connection.IsConnected && IsLoading;

    [RelayCommand(CanExecute = nameof(CanApplyParameters))]
    private Task ApplyParametersAsync()
    {
        if (ActiveDocument is not { IsDashboard: true, Dashboard.Current: { } page } document)
            return Task.CompletedTask;

        document.Dashboard.SnapshotValues();
        return LoadCardsAsync(document, page.Cards);
    }

    /// <summary>Interrompt la lecture des cartes en cours : elles gardent leur dernier resultat.</summary>
    [RelayCommand(CanExecute = nameof(CanCancelLoading))]
    private void CancelLoading() => _cancellation?.Cancel();

    [RelayCommand]
    private void Back() => ActiveDocument?.Dashboard.Back();

    /// <summary>Les etapes du fil d'Ariane gardent leurs resultats : rien n'est relu.</summary>
    [RelayCommand]
    private void GoToStep(DashboardPageState? step)
    {
        if (step is not null)
            ActiveDocument?.Dashboard.GoTo(step);
    }

    /// <summary>Bouton, ou ligne cliquee : les parametres prennent les valeurs prevues et la page cible s'ouvre.</summary>
    private void OnNavigationRequested(object? sender, DashboardNavigationRequest request)
    {
        if (FindDocument(sender) is not { Dashboard: { Definition: { } definition } session } document
            || request.Card.Definition.Navigation is not { } navigation)
        {
            return;
        }

        // Pas de navigation dans le concepteur : un clic y sert a selectionner.
        if (session.IsDesigning)
            return;

        if (definition.FindPage(navigation.Page) is not { } page)
        {
            Status.Set($"La page « {navigation.Page} » n'existe pas dans ce dashboard.", isError: true);
            return;
        }

        session.SnapshotValues();
        session.ApplyValues(navigation.Resolve(session.CurrentValues(), request.Card.Result, request.Row));
        OpenPage(document, page, asRoot: false);
    }

    private void OpenPage(EditorDocument document, DashboardPageDefinition page, bool asRoot)
    {
        var state = document.Dashboard.Open(page, asRoot);

        // Une page est maintenant affichee : Executer peut s'activer.
        RefreshCanExecute();
        _ = LoadCardsAsync(document, state.Cards);
    }

    // ---------- Lecture des cartes ----------

    /// <summary>Cartes lues en parallele, chacune affichee des qu'elle arrive ; une nouvelle lecture annule la precedente.</summary>
    private async Task LoadCardsAsync(EditorDocument document, IEnumerable<DashboardCardState> cards)
    {
        var session = document.Dashboard;
        var queried = cards.Where(c => c.UsesQuery).ToList();
        if (queried.Count == 0)
            return;

        _activeLoads++;
        IsLoading = true;
        try
        {
            await LoadQueriedCardsAsync(session, queried);
        }
        finally
        {
            _activeLoads--;
            IsLoading = _activeLoads > 0;
        }
    }

    private async Task LoadQueriedCardsAsync(DashboardSession session, List<DashboardCardState> queried)
    {
        _cancellation?.Cancel();
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(_workspace.LifetimeToken);
        var token = _cancellation.Token;

        List<DashboardQueryParameter> parameters = [];
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
        var connection = _workspace.Connection.ActiveConnection
            ?? throw new InvalidOperationException("Choisissez une connexion pour alimenter les cartes.");

        if (!await _connectionStore.OpenConnectionAsync(connection, token))
            throw new InvalidOperationException($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.");

        return connection;
    }

    // ---------- Concepteur : cartes ----------

    [RelayCommand]
    private void SelectCard(DashboardCardState? card)
    {
        if (ActiveDocument is { IsDashboardDesigning: true } document)
            document.Dashboard.SelectedCard = card;
    }

    /// <summary>Nouvelle carte de 4 × 2 cases (reduite au bord droit de la grille).</summary>
    [RelayCommand]
    private void AddCard(DashboardCell? cell)
    {
        if (ActiveDocument is not { IsDashboardDesigning: true, Dashboard: { Current: not null } session } || cell is null)
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
        if (ActiveDocument is { IsDashboardDesigning: true } document && card is not null)
        {
            document.Dashboard.DraggedCard = card;
            document.Dashboard.SelectedCard = card;
        }
    }

    [RelayCommand]
    private void DropCard(DashboardCell? cell)
    {
        if (ActiveDocument is not { IsDashboardDesigning: true, Dashboard: { DraggedCard: { } card } session } || cell is null)
            return;

        session.DraggedCard = null;
        card.Column = Math.Clamp(cell.Column, 0, session.ColumnCount - card.ColumnSpan);
        card.Row = cell.Row;
    }

    [RelayCommand]
    private void MoveCard(string? direction)
    {
        if (ActiveDocument is not { Dashboard: { SelectedCard: { } card } session })
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
        if (ActiveDocument is not { Dashboard: { SelectedCard: { } card } session })
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
        if (ActiveDocument is { IsDashboard: true } document && card is not null)
            document.Dashboard.RemoveCard(card);
    }

    [RelayCommand]
    private Task RunCardAsync(DashboardCardState? card)
        => ActiveDocument is { IsDashboard: true } document && card is not null
            ? LoadCardsAsync(document, [card])
            : Task.CompletedTask;

    // ---------- Concepteur : pages et parametres ----------

    [RelayCommand]
    private void OpenDesignerPage(DashboardPageDefinition? page)
    {
        if (ActiveDocument is { IsDashboard: true } document && page is not null)
            OpenPage(document, page, asRoot: true);
    }

    [RelayCommand]
    private void AddPage()
    {
        if (ActiveDocument is not { IsDashboard: true, Dashboard: { Definition: { } definition } session } document)
            return;

        var number = definition.Pages.Count + 1;
        while (definition.FindPage($"page{number.ToString(CultureInfo.InvariantCulture)}") is not null)
            number++;

        var page = new DashboardPageDefinition { Id = $"page{number.ToString(CultureInfo.InvariantCulture)}", Title = "Nouvelle page" };
        session.AddPage(page);
        OpenPage(document, page, asRoot: true);
    }

    /// <summary>Supprime la page affichee (la derniere page restante est conservee).</summary>
    [RelayCommand]
    private void DeletePage()
    {
        if (ActiveDocument is not { IsDashboard: true, Dashboard: { Definition: { } definition, Current: { } current } session } document)
            return;

        if (definition.Pages.Count <= 1)
        {
            Status.Set("Un dashboard garde au moins une page.", isError: true);
            return;
        }

        session.RemovePage(current.Definition);
        OpenPage(document, definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    [RelayCommand]
    private void AddParameter()
    {
        if (ActiveDocument is not { IsDashboard: true, Dashboard: { Definition: { } definition } session })
            return;

        var number = definition.Parameters.Count + 1;
        while (definition.Parameters.Any(p => string.Equals(p.Name, $"Param{number.ToString(CultureInfo.InvariantCulture)}", StringComparison.OrdinalIgnoreCase)))
            number++;

        session.AddParameter(new DashboardParameterDefinition { Name = $"Param{number.ToString(CultureInfo.InvariantCulture)}" });
    }

    [RelayCommand]
    private void DeleteParameter(DashboardParameterValue? parameter)
    {
        if (ActiveDocument is { IsDashboard: true } document && parameter is not null)
            document.Dashboard.RemoveParameter(parameter);
    }
}
