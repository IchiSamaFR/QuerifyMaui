using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Models.Editors;
using System.Collections.ObjectModel;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>
/// Etat d'un onglet .schx : vue affichee, definition lue, valeurs des parametres, pile des pages ouvertes
/// (fil d'Ariane), lecture des cartes et, dans le concepteur, la carte selectionnee et les cases libres de la grille.
/// </summary>
public sealed partial class DashboardSession : AbstractSession
{
    public DashboardSession(EditorDocument document)
        : base(document)
    {
    }

    /// <summary>Un bouton ou une ligne de tableau demande l'ouverture d'une page.</summary>
    public event EventHandler<DashboardNavigationRequest>? NavigationRequested;

    /// <summary>Le concepteur a modifie la definition : l'hote reecrit le fichier.</summary>
    public event EventHandler? DefinitionChanged;

    public ObservableCollection<DashboardParameterValue> Parameters { get; } = [];

    /// <summary>Fil d'Ariane : la derniere etape est la page affichee.</summary>
    public ObservableCollection<DashboardPageState> Steps { get; } = [];

    /// <summary>Pages du fichier (choix de la page editee, cible des boutons).</summary>
    public ObservableCollection<DashboardPageDefinition> Pages { get; } = [];

    /// <summary>Identifiants des pages, precedes d'un vide (« aucune ») : cible d'un bouton dans le concepteur.</summary>
    public ObservableCollection<string> PageIds { get; } = [];

    /// <summary>Cases de la grille, affichees par le concepteur.</summary>
    public ObservableCollection<DashboardCell> Cells { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDefinition), nameof(ColumnCount))]
    public partial DashboardDefinition? Definition { get; private set; }

    /// <summary>Fichier illisible, ou probleme de saisie / d'ouverture ; null si tout va bien.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrent), nameof(CanGoBack))]
    public partial DashboardPageState? Current { get; private set; }

    /// <summary>Consultation, concepteur ou JSON.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDesigning), nameof(IsSourceMode), nameof(IsGridMode))]
    public partial DashboardMode Mode { get; set; }

    public bool IsDesigning => Mode == DashboardMode.Design;

    public bool IsSourceMode => Mode == DashboardMode.Source;

    public bool IsGridMode => Mode != DashboardMode.Source;

    /// <summary>Vrai tant qu'une lecture de cartes est en cours.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    /// <summary>Lectures en cours : une nouvelle peut demarrer avant la fin de l'annulation de la precedente.</summary>
    private int _activeLoads;

    /// <summary>Lecture de cartes en cours ; la remplacer annule la precedente.</summary>
    private CancellationTokenSource? _loadCancellation;

    [ObservableProperty]
    public partial DashboardCardState? SelectedCard { get; set; }

    /// <summary>Lignes de la grille : le minimum du fichier, allonge par les cartes (et d'une ligne libre dans le concepteur).</summary>
    [ObservableProperty]
    public partial int RowCount { get; private set; } = DashboardDefinition.DEFAULT_ROWS;

    /// <summary>Carte en cours de glisser-deposer dans le concepteur.</summary>
    public DashboardCardState? DraggedCard { get; set; }

    /// <summary>Texte du fichier lu ou ecrit en dernier : un autre texte demande une nouvelle lecture.</summary>
    public string? LoadedText { get; private set; }

    public bool HasDefinition => Definition is not null;

    public bool HasError => Error is not null;

    public bool HasCurrent => Current is not null;

    public bool CanGoBack => Steps.Count > 1;

    public bool HasParameters => Parameters.Count > 0;

    public int ColumnCount => Math.Max(1, Definition?.Columns ?? DashboardDefinition.DEFAULT_COLUMNS);

    // ---------- Chargement ----------

    /// <summary>Repart d'une nouvelle definition (ou d'aucune si le fichier est illisible).</summary>
    public void Load(DashboardDefinition? definition, string text, string? error)
    {
        Steps.Clear();
        Current = null;
        SelectedCard = null;
        Definition = definition;
        Error = error;
        LoadedText = text;

        Parameters.Clear();
        Pages.Clear();
        foreach (var parameter in definition?.Parameters ?? [])
            Parameters.Add(Wire(new DashboardParameterValue(parameter)));
        foreach (var page in definition?.Pages ?? [])
            Pages.Add(page);
        RefreshPageIds();

        OnPropertyChanged(nameof(HasParameters));
        RefreshLayout();
    }

    /// <summary>Le texte ecrit par le concepteur correspond deja a l'etat affiche.</summary>
    public void MarkWritten(string text) => LoadedText = text;

    // ---------- Pages ----------

    public IReadOnlyDictionary<string, string?> CurrentValues()
        => Parameters.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (string?)g.First().Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>Ouvre une page avec les valeurs courantes ; <paramref name="asRoot"/> repart d'un fil d'Ariane vide.</summary>
    public DashboardPageState Open(DashboardPageDefinition page, bool asRoot)
    {
        var state = new DashboardPageState(page, CurrentValues());
        state.DefinitionChanged += OnPageDefinitionChanged;
        foreach (var card in state.Cards)
            Wire(card);

        if (asRoot)
            Steps.Clear();

        Steps.Add(state);
        Current = state;
        SelectedCard = null;
        OnPropertyChanged(nameof(CanGoBack));
        RefreshLayout();
        return state;
    }

    /// <summary>Revient a une etape du fil d'Ariane et retrouve ses valeurs de parametres.</summary>
    public bool GoTo(DashboardPageState step)
    {
        var index = Steps.IndexOf(step);
        if (index < 0)
            return false;

        while (Steps.Count > index + 1)
            Steps.RemoveAt(Steps.Count - 1);

        ApplyValues(step.ParameterValues);
        Current = step;
        SelectedCard = null;
        OnPropertyChanged(nameof(CanGoBack));
        RefreshLayout();
        return true;
    }

    public void Back()
    {
        if (Steps.Count > 1)
            GoTo(Steps[^2]);
    }

    /// <summary>Donne des valeurs aux parametres (les autres sont inchanges).</summary>
    public void ApplyValues(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (var parameter in Parameters)
        {
            if (values.TryGetValue(parameter.Name, out var value))
            {
                parameter.Value = value ?? string.Empty;
                parameter.Error = null;
            }
        }
    }

    /// <summary>Les valeurs saisies deviennent celles de l'etape courante (retour depuis une page suivante).</summary>
    public void SnapshotValues()
    {
        if (Current is null)
            return;

        Current.ParameterValues.Clear();
        foreach (var (name, value) in CurrentValues())
            Current.ParameterValues[name] = value;
    }

    // ---------- Concepteur ----------

    partial void OnModeChanged(DashboardMode oldValue, DashboardMode newValue)
    {
        if ((oldValue == DashboardMode.Design) == (newValue == DashboardMode.Design))
            return;

        if (!IsDesigning)
            SelectedCard = null;
        RefreshLayout();
    }

    // ---------- Lecture des cartes ----------

    /// <summary>Demarre une lecture : annule la precedente et rend le jeton de la nouvelle (annule aussi a la fermeture).</summary>
    public CancellationToken BeginLoad()
    {
        // Pas de Dispose : la lecture remplacee peut encore utiliser son jeton.
        _loadCancellation?.Cancel();
        _loadCancellation = CancellationTokenSource.CreateLinkedTokenSource(LifetimeToken);

        _activeLoads++;
        IsLoading = true;
        return _loadCancellation.Token;
    }

    public void EndLoad()
    {
        _activeLoads = Math.Max(0, _activeLoads - 1);
        IsLoading = _activeLoads > 0;
    }

    /// <summary>Interrompt la lecture en cours : les cartes gardent leur dernier resultat.</summary>
    public void CancelLoad() => _loadCancellation?.Cancel();

    partial void OnSelectedCardChanged(DashboardCardState? oldValue, DashboardCardState? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;
        if (newValue is not null)
            newValue.IsSelected = true;
    }

    public DashboardCardState AddCard(DashboardCardDefinition card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var page = Current ?? throw new InvalidOperationException("Aucune page ouverte.");

        page.Definition.Cards.Add(card);
        var state = Wire(new DashboardCardState(card));
        page.Cards.Add(state);
        SelectedCard = state;
        NotifyDefinitionChanged();
        return state;
    }

    public void RemoveCard(DashboardCardState card)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (Current is not { } page || !page.Cards.Remove(card))
            return;

        page.Definition.Cards.Remove(card.Definition);
        if (SelectedCard == card)
            SelectedCard = null;
        NotifyDefinitionChanged();
    }

    public void AddPage(DashboardPageDefinition page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (Definition is null)
            return;

        Definition.Pages.Add(page);
        Pages.Add(page);
        RefreshPageIds();
        NotifyDefinitionChanged();
    }

    public void RemovePage(DashboardPageDefinition page)
    {
        if (Definition is null || Definition.Pages.Count <= 1 || !Definition.Pages.Remove(page))
            return;

        Pages.Remove(page);
        RefreshPageIds();
        if (string.Equals(Definition.StartPage, page.Id, StringComparison.OrdinalIgnoreCase))
            Definition.StartPage = Definition.Pages[0].Id;
        NotifyDefinitionChanged();
    }

    public void AddParameter(DashboardParameterDefinition parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (Definition is null)
            return;

        Definition.Parameters.Add(parameter);
        Parameters.Add(Wire(new DashboardParameterValue(parameter)));
        OnPropertyChanged(nameof(HasParameters));
        NotifyDefinitionChanged();
    }

    public void RemoveParameter(DashboardParameterValue parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (Definition is null || !Parameters.Remove(parameter))
            return;

        Definition.Parameters.Remove(parameter.Definition);
        OnPropertyChanged(nameof(HasParameters));
        NotifyDefinitionChanged();
    }

    /// <summary>Recalcule le nombre de lignes et, dans le concepteur, les cases de la grille.</summary>
    public void RefreshLayout()
    {
        var rows = Math.Max(Definition?.Rows ?? DashboardDefinition.DEFAULT_ROWS, Current?.BottomRow ?? 0);
        if (IsDesigning)
            rows++;

        if (RowCount != rows || (IsDesigning && Cells.Count != rows * ColumnCount) || (!IsDesigning && Cells.Count > 0))
        {
            RowCount = rows;
            Cells.Clear();
            if (IsDesigning)
            {
                for (var row = 0; row < rows; row++)
                {
                    for (var column = 0; column < ColumnCount; column++)
                        Cells.Add(new DashboardCell(column, row));
                }
            }
        }
    }

    /// <summary>Mise a jour sans vider la liste : un Picker lie perdrait sa selection (et la reecrirait a null).</summary>
    private void RefreshPageIds()
    {
        if (PageIds.Count == 0)
            PageIds.Add(string.Empty);

        for (var i = PageIds.Count - 1; i > 0; i--)
        {
            if (!Pages.Any(p => string.Equals(p.Id, PageIds[i], StringComparison.Ordinal)))
                PageIds.RemoveAt(i);
        }

        foreach (var page in Pages.Where(p => !PageIds.Contains(p.Id)))
            PageIds.Add(page.Id);
    }

    private DashboardCardState Wire(DashboardCardState card)
    {
        card.DefinitionChanged += OnCardDefinitionChanged;
        card.NavigationRequested += OnCardNavigationRequested;
        return card;
    }

    private DashboardParameterValue Wire(DashboardParameterValue parameter)
    {
        parameter.DefinitionChanged += OnParameterDefinitionChanged;
        return parameter;
    }

    private void OnCardDefinitionChanged(object? sender, EventArgs e) => NotifyDefinitionChanged();

    private void OnParameterDefinitionChanged(object? sender, EventArgs e) => NotifyDefinitionChanged();

    private void OnPageDefinitionChanged(object? sender, EventArgs e) => NotifyDefinitionChanged();

    private void OnCardNavigationRequested(object? sender, DashboardNavigationRequest e) => NavigationRequested?.Invoke(this, e);

    private void NotifyDefinitionChanged()
    {
        RefreshLayout();
        DefinitionChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _loadCancellation?.Cancel();

        base.Dispose(disposing);
    }
}
