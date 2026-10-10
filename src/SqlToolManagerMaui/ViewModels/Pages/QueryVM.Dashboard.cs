using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Enums.Editors;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Databases.Dashboards;
using System.Collections.Specialized;
using System.Globalization;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>
/// Partie "dashboard" : onglets .schx. Le JSON du fichier reste la source : le concepteur ecrit
/// dans la definition puis le texte est reserialise ; la vue Source est relue au retour.
/// </summary>
public partial class QueryVM
{
    /// <summary>Lignes lues par carte (tableau ou graphique).</summary>
    private const int CARD_MAX_ROWS = 1000;

    private CancellationTokenSource? _dashboardCancellation;
    private int _untitledDashboardCount;

    // ---------- Onglets ----------

    private void OnDocumentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var document in e.OldItems?.OfType<EditorDocument>().Where(d => d.IsDashboard) ?? [])
        {
            document.Dashboard.DefinitionChanged -= OnDashboardDefinitionChanged;
            document.Dashboard.NavigationRequested -= OnDashboardNavigationRequested;
        }

        foreach (var document in e.NewItems?.OfType<EditorDocument>().Where(d => d.IsDashboard) ?? [])
        {
            document.Dashboard.DefinitionChanged += OnDashboardDefinitionChanged;
            document.Dashboard.NavigationRequested += OnDashboardNavigationRequested;
        }
    }

    private EditorDocument? FindDashboardDocument(object? session)
        => Documents.FirstOrDefault(d => ReferenceEquals(d.Dashboard, session));

    /// <summary>Nouveau dashboard : une page vide, ouverte dans le concepteur, sur la connexion active.</summary>
    [RelayCommand]
    private void NewDashboard()
    {
        _untitledDashboardCount++;
        var definition = DashboardDefinition.CreateEmpty(ActiveConnection?.Name, database: null);

        // Texte « enregistre » vide : l'onglet reste modifie tant qu'il n'a pas ete enregistre.
        var document = new EditorDocument($"Dashboard {_untitledDashboardCount.ToString(CultureInfo.InvariantCulture)}", filePath: null, fileText: string.Empty, SyntaxLanguage.None, DocumentKind.Dashboard)
        {
            EditorText = DashboardSerializer.Serialize(definition),
            DashboardMode = DashboardMode.Design,
        };

        OpenDashboard(document);
    }

    /// <summary>Ajoute l'onglet, lit le fichier, propose la connexion qu'il nomme et ouvre la premiere page.</summary>
    private void OpenDashboard(EditorDocument document)
    {
        Documents.Add(document);
        ActiveDocument = document;

        LoadDashboard(document, force: true);

        if (document.Dashboard.Definition?.Connection is { } name
            && Connections.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) is { } connection)
        {
            SelectedConnection = connection;
        }

        SetStatus(document.Dashboard.HasError
            ? $"{document.FileName} : fichier invalide, corrigez-le dans la vue Source."
            : $"{document.FileName} ouvert.", isError: document.Dashboard.HasError);
    }

    /// <summary>Relit le fichier s'il a change depuis la derniere lecture ou ecriture, puis ouvre la page de depart.</summary>
    private void LoadDashboard(EditorDocument document, bool force = false)
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
            OpenDashboardPage(document, definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    // ---------- Bascule Dashboard / Concepteur / Source ----------

    [RelayCommand]
    private void ShowDashboardView() => SetDashboardMode(DashboardMode.View);

    [RelayCommand]
    private void ShowDashboardDesigner() => SetDashboardMode(DashboardMode.Design);

    [RelayCommand]
    private void ShowDashboardSource() => SetDashboardMode(DashboardMode.Source);

    private void SetDashboardMode(DashboardMode mode)
    {
        if (ActiveDocument is not { IsDashboard: true } document || document.DashboardMode == mode)
            return;

        document.DashboardMode = mode;
        document.Dashboard.IsDesigning = mode == DashboardMode.Design;

        // Retour depuis la vue Source : le JSON a pu etre modifie a la main.
        if (mode != DashboardMode.Source)
            LoadDashboard(document);
    }

    /// <summary>Le concepteur a modifie la definition : le fichier est reecrit (l'onglet passe en modifie).</summary>
    private void OnDashboardDefinitionChanged(object? sender, EventArgs e)
    {
        if (FindDashboardDocument(sender) is not { Dashboard.Definition: { } definition } document)
            return;

        var text = DashboardSerializer.Serialize(definition);
        document.Dashboard.MarkWritten(text);
        document.EditorText = text;
    }

    // ---------- Consultation ----------

    [RelayCommand]
    private Task ApplyDashboardParametersAsync()
    {
        if (ActiveDocument is not { IsDashboard: true, Dashboard.Current: { } page } document)
            return Task.CompletedTask;

        document.Dashboard.SnapshotValues();
        return LoadDashboardCardsAsync(document, page.Cards);
    }

    [RelayCommand]
    private void DashboardBack() => ActiveDocument?.Dashboard.Back();

    /// <summary>Les etapes du fil d'Ariane gardent leurs resultats : rien n'est relu.</summary>
    [RelayCommand]
    private void DashboardGoToStep(DashboardPageState? step)
    {
        if (step is not null)
            ActiveDocument?.Dashboard.GoTo(step);
    }

    /// <summary>Bouton, ou ligne cliquee : les parametres prennent les valeurs prevues et la page cible s'ouvre.</summary>
    private void OnDashboardNavigationRequested(object? sender, DashboardNavigationRequest request)
    {
        if (FindDashboardDocument(sender) is not { Dashboard: { Definition: { } definition } session } document
            || request.Card.Definition.Navigation is not { } navigation)
        {
            return;
        }

        // Pas de navigation dans le concepteur : un clic y sert a selectionner.
        if (session.IsDesigning)
            return;

        if (definition.FindPage(navigation.Page) is not { } page)
        {
            SetStatus($"La page « {navigation.Page} » n'existe pas dans ce dashboard.", isError: true);
            return;
        }

        session.SnapshotValues();
        session.ApplyValues(navigation.Resolve(session.CurrentValues(), request.Card.Result, request.Row));
        OpenDashboardPage(document, page, asRoot: false);
    }

    private void OpenDashboardPage(EditorDocument document, DashboardPageDefinition page, bool asRoot)
    {
        var state = document.Dashboard.Open(page, asRoot);
        _ = LoadDashboardCardsAsync(document, state.Cards);
    }

    // ---------- Lecture des cartes ----------

    /// <summary>Cartes lues en parallele, chacune affichee des qu'elle arrive ; une nouvelle lecture annule la precedente.</summary>
    private async Task LoadDashboardCardsAsync(EditorDocument document, IEnumerable<DashboardCardState> cards)
    {
        var session = document.Dashboard;
        var queried = cards.Where(c => c.UsesQuery).ToList();
        if (queried.Count == 0)
            return;

        _dashboardCancellation?.Cancel();
        _dashboardCancellation = CancellationTokenSource.CreateLinkedTokenSource(LifetimeToken);
        var token = _dashboardCancellation.Token;

        List<DashboardQueryParameter> parameters = [];
        var isValid = true;
        foreach (var parameter in session.Parameters)
        {
            isValid &= parameter.TryGetQueryParameter(out var value);
            parameters.Add(value);
        }

        if (!isValid)
        {
            SetStatus("Paramètres invalides : corrigez la saisie puis appliquez.", isError: true);
            return;
        }

        DatabaseConnectionInfo connection;
        try
        {
            connection = await OpenDashboardConnectionAsync(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (InvalidOperationException ex)
        {
            foreach (var card in queried)
                card.Error = ex.Message;
            SetStatus(ex.Message, isError: true);
            return;
        }

        var database = session.Definition?.Database;
        await Task.WhenAll(queried.Select(card => LoadDashboardCardAsync(connection, database, card, parameters, token)));
    }

    private async Task LoadDashboardCardAsync(DatabaseConnectionInfo connection, string? database, DashboardCardState card, IReadOnlyList<DashboardQueryParameter> parameters, CancellationToken token)
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

    private async Task<DatabaseConnectionInfo> OpenDashboardConnectionAsync(CancellationToken token)
    {
        var connection = ActiveConnection
            ?? throw new InvalidOperationException("Choisissez une connexion pour alimenter les cartes.");

        if (!await _connectionStore.OpenConnectionAsync(connection, token))
            throw new InvalidOperationException($"La chaîne de connexion de {connection.Name} est introuvable : réenregistrez-la depuis la page Connexions.");

        return connection;
    }

    // ---------- Concepteur : cartes ----------

    [RelayCommand]
    private void SelectDashboardCard(DashboardCardState? card)
    {
        if (ActiveDocument is { IsDashboardDesigning: true } document)
            document.Dashboard.SelectedCard = card;
    }

    /// <summary>Nouvelle carte de 4 × 2 cases (reduite au bord droit de la grille).</summary>
    [RelayCommand]
    private void AddDashboardCard(DashboardCell? cell)
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
    private void StartDashboardDrag(DashboardCardState? card)
    {
        if (ActiveDocument is { IsDashboardDesigning: true } document && card is not null)
        {
            document.Dashboard.DraggedCard = card;
            document.Dashboard.SelectedCard = card;
        }
    }

    [RelayCommand]
    private void DropDashboardCard(DashboardCell? cell)
    {
        if (ActiveDocument is not { IsDashboardDesigning: true, Dashboard: { DraggedCard: { } card } session } || cell is null)
            return;

        session.DraggedCard = null;
        card.Column = Math.Clamp(cell.Column, 0, session.ColumnCount - card.ColumnSpan);
        card.Row = cell.Row;
    }

    [RelayCommand]
    private void MoveDashboardCard(string? direction)
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
    private void ResizeDashboardCard(string? change)
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
    private void DeleteDashboardCard(DashboardCardState? card)
    {
        if (ActiveDocument is { IsDashboard: true } document && card is not null)
            document.Dashboard.RemoveCard(card);
    }

    [RelayCommand]
    private Task RunDashboardCardAsync(DashboardCardState? card)
        => ActiveDocument is { IsDashboard: true } document && card is not null
            ? LoadDashboardCardsAsync(document, [card])
            : Task.CompletedTask;

    // ---------- Concepteur : pages et parametres ----------

    [RelayCommand]
    private void OpenDashboardDesignerPage(DashboardPageDefinition? page)
    {
        if (ActiveDocument is { IsDashboard: true } document && page is not null)
            OpenDashboardPage(document, page, asRoot: true);
    }

    [RelayCommand]
    private void AddDashboardPage()
    {
        if (ActiveDocument is not { IsDashboard: true, Dashboard: { Definition: { } definition } session } document)
            return;

        var number = definition.Pages.Count + 1;
        while (definition.FindPage($"page{number.ToString(CultureInfo.InvariantCulture)}") is not null)
            number++;

        var page = new DashboardPageDefinition { Id = $"page{number.ToString(CultureInfo.InvariantCulture)}", Title = "Nouvelle page" };
        session.AddPage(page);
        OpenDashboardPage(document, page, asRoot: true);
    }

    /// <summary>Supprime la page affichee (la derniere page restante est conservee).</summary>
    [RelayCommand]
    private void DeleteDashboardPage()
    {
        if (ActiveDocument is not { IsDashboard: true, Dashboard: { Definition: { } definition, Current: { } current } session } document)
            return;

        if (definition.Pages.Count <= 1)
        {
            SetStatus("Un dashboard garde au moins une page.", isError: true);
            return;
        }

        session.RemovePage(current.Definition);
        OpenDashboardPage(document, definition.FindPage(definition.StartPage) ?? definition.Pages[0], asRoot: true);
    }

    [RelayCommand]
    private void AddDashboardParameter()
    {
        if (ActiveDocument is not { IsDashboard: true, Dashboard: { Definition: { } definition } session })
            return;

        var number = definition.Parameters.Count + 1;
        while (definition.Parameters.Any(p => string.Equals(p.Name, $"Param{number.ToString(CultureInfo.InvariantCulture)}", StringComparison.OrdinalIgnoreCase)))
            number++;

        session.AddParameter(new DashboardParameterDefinition { Name = $"Param{number.ToString(CultureInfo.InvariantCulture)}" });
    }

    [RelayCommand]
    private void DeleteDashboardParameter(DashboardParameterValue? parameter)
    {
        if (ActiveDocument is { IsDashboard: true } document && parameter is not null)
            document.Dashboard.RemoveParameter(parameter);
    }
}
