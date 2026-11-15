using CommunityToolkit.Maui;
using Querify.Core.Enums.Dashboards;
using Querify.Core.Models.Dashboards;
using Querify.Core.Models.Editors.Highlighting;
using Querify.Core.Models.Settings;
using Querify.Databases.Models.Connections;
using System.Windows.Input;
using Querify.Diagnostics;
using Querify.UI.Extensions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Querify.Views.Controls.Dashboards;

/// <summary>
/// Dashboard d'un onglet .schx : parametres, fil d'Ariane et grille de cartes. Dans le concepteur, les onglets
/// des pages remplacent le fil d'Ariane, les parametres se declarent dans leur barre, les cases libres recoivent
/// les cartes ajoutees ou glissees et le panneau du bas edite la carte selectionnee.
/// </summary>
public partial class DashboardGrid : ContentView
{
    /// <summary>Rendus proposes pour une carte.</summary>
    public static DashboardCardKind[] CardKinds { get; } = Enum.GetValues<DashboardCardKind>();

    [BindableProperty(PropertyChangedMethodName = nameof(OnSessionChanged))]
    public partial DashboardSession? Session { get; set; }

    [BindableProperty]
    public partial SyntaxConfiguration? SyntaxConfiguration { get; set; }

    /// <summary>Coloration et autocompletion des cartes LINQ (EF).</summary>
    [BindableProperty]
    public partial SyntaxConfiguration? LinqSyntaxConfiguration { get; set; }

    [BindableProperty]
    public partial EditorPreferences? EditorPreferences { get; set; }

    [BindableProperty]
    public partial DatabaseConnectionInfo? Connection { get; set; }

    [BindableProperty]
    public partial bool IsDashboardParamsOpen { get; set; }

    /// <summary>Nombre de lignes de la grille (les definitions de lignes sont creees en code).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnGridSizeChanged))]
    public partial int RowCount { get; set; } = 6;

    [BindableProperty(PropertyChangedMethodName = nameof(OnGridSizeChanged))]
    public partial int ColumnCount { get; set; } = 12;

    /// <summary>Hauteur d'une ligne de la grille.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnGridSizeChanged))]
    public partial double RowHeight { get; set; } = 140d;

    /// <summary>Relit la carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? RunCardCommand { get; set; }

    /// <summary>Applique les valeurs saisies et relit les cartes.</summary>
    [BindableProperty]
    public partial ICommand? ApplyParametersCommand { get; set; }

    [BindableProperty]
    public partial ICommand? BackCommand { get; set; }

    /// <summary>Revient a une etape du fil d'Ariane (parametre : <see cref="DashboardPageState"/>).</summary>
    [BindableProperty]
    public partial ICommand? GoToStepCommand { get; set; }

    /// <summary>Concepteur : selectionne une carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? SelectCardCommand { get; set; }

    /// <summary>Concepteur : carte selectionnee, cible des touches Echap (deselection) et Suppr (suppression).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectedCardChanged))]
    public partial DashboardCardState? SelectedCard { get; set; }

    /// <summary>Concepteur : supprime une carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? DeleteCardCommand { get; set; }

    /// <summary>Concepteur : ajoute une carte dans une case libre (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? AddCardCommand { get; set; }

    /// <summary>Concepteur : debut du glisser d'une carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? StartDragCommand { get; set; }

    /// <summary>Concepteur : depot de la carte glissee sur une case (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? DropCommand { get; set; }

    /// <summary>Concepteur : la carte glissee survole une case (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? DragOverCommand { get; set; }

    /// <summary>Concepteur : la carte glissee quitte une case (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? DragLeaveCommand { get; set; }

    /// <summary>Concepteur : fin du glisser, depose ou non.</summary>
    [BindableProperty]
    public partial ICommand? EndDragCommand { get; set; }

    /// <summary>Concepteur : ouvre une page (parametre : <see cref="DashboardPageDefinition"/>).</summary>
    [BindableProperty]
    public partial ICommand? OpenPageCommand { get; set; }

    [BindableProperty]
    public partial ICommand? AddPageCommand { get; set; }

    /// <summary>Concepteur : supprime une page (parametre : <see cref="DashboardPageDefinition"/>).</summary>
    [BindableProperty]
    public partial ICommand? DeletePageCommand { get; set; }

    /// <summary>Concepteur : declare un nouveau parametre.</summary>
    [BindableProperty]
    public partial ICommand? AddParameterCommand { get; set; }

    /// <summary>Concepteur : modifie un parametre (parametre : <see cref="DashboardParameterValue"/>).</summary>
    [BindableProperty]
    public partial ICommand? EditParameterCommand { get; set; }

    /// <summary>Concepteur : supprime un parametre (parametre : <see cref="DashboardParameterValue"/>).</summary>
    [BindableProperty]
    public partial ICommand? DeleteParameterCommand { get; set; }

    private readonly DashboardCellsDrawable _cells = new()
    {
        StrokeColor = ResourceColor("DividerStrokeColorDefault", Colors.Gray),
        HoverColor = ResourceColor("SubtleHoverOverlay", Colors.Transparent),
        GlyphColor = ResourceColor("TextFillColorTertiary", Colors.Gray),
    };

    private DashboardSession? _observedSession;
    private ObservableCollection<DashboardCardState>? _sourceCards;

    /// <summary>Incremente a chaque changement de page : un remplissage en cours d'une page quittee s'arrete.</summary>
    private int _fillVersion;

    /// <summary>Gabarit d'une vue de carte (ressource du XAML).</summary>
    private DataTemplate? _cardTemplate;

    /// <summary>Rendu de la derniere carte portee par chaque vue : une vue libre sert d'abord une carte du meme rendu.</summary>
    private readonly Dictionary<View, DashboardCardKind> _viewKinds = [];

    /// <summary>Fermeture de l'onglet observe : ses cartes sont alors liberees.</summary>
    private CancellationTokenRegistration _sessionClosed;

    /// <summary>Case visee par la carte glissee, pour ne relayer que les changements de case.</summary>
    private DashboardCell? _dragCell;

    /// <summary>Position du pointeur dans la carte au debut du glisser : l'apercu suit le coin de la carte, pas la souris.</summary>
    private Point _grabOffset;

    public DashboardGrid()
    {
        InitializeComponent();
        CellsCanvas.Drawable = _cells;
        ApplyGridSize();
    }

    private static void OnGridSizeChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DashboardGrid)bindable).ApplyGridSize();

    // ---------- Affichage des cartes (vues reutilisees) ----------

    private static void OnSessionChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DashboardGrid)bindable).ObserveSession(newValue as DashboardSession);

    private void ObserveSession(DashboardSession? session)
    {
        // Vue masquee (onglet d'un autre type) : les cartes restent en place, revenir sur le meme onglet ne recree rien.
        if (session is null)
            return;

        if (!ReferenceEquals(session, _observedSession))
        {
            StopObserving();
            _observedSession = session;
            session.PropertyChanged += OnObservedSessionPropertyChanged;
            _sessionClosed = session.LifetimeToken.Register(() => Dispatcher.Dispatch(() => ReleaseSession(session)));
        }

        ShowCards(session.Current?.Cards);
    }

    private void StopObserving()
    {
        _sessionClosed.Dispose();
        _sessionClosed = default;

        if (_observedSession is not null)
            _observedSession.PropertyChanged -= OnObservedSessionPropertyChanged;

        _observedSession = null;
    }

    /// <summary>Onglet ferme alors que la vue est masquee : ses cartes et leurs resultats ne sont plus retenus.</summary>
    private void ReleaseSession(DashboardSession session)
    {
        if (!ReferenceEquals(session, _observedSession))
            return;

        StopObserving();
        ShowCards(null);
    }

    private void OnObservedSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardSession.Current))
            ShowCards(_observedSession?.Current?.Cards);
    }

    /// <summary>Change de jeu de cartes : les vues en place sont reliees aux nouvelles cartes, seules les manquantes sont creees.</summary>
    private void ShowCards(ObservableCollection<DashboardCardState>? cards, bool force = false)
    {
        if (!force && ReferenceEquals(cards, _sourceCards))
            return;

        if (_sourceCards is not null)
            _sourceCards.CollectionChanged -= OnSourceCardsChanged;

        _sourceCards = cards;
        if (cards is not null)
            cards.CollectionChanged += OnSourceCardsChanged;

        FillCardsAsync(++_fillVersion).FireAndForget("Affichage des cartes du dashboard en échec.");
    }

    /// <summary>
    /// Creer une vue de carte coute bien plus cher que la relier : celles deja creees sont reprises, les autres
    /// ajoutees une par une en rendant la main au thread UI entre deux (toutes d'un bloc, l'application figeait).
    /// </summary>
    private async Task FillCardsAsync(int version)
    {
        var missing = _sourceCards?.ToList() ?? [];
        List<View> free = [];
        foreach (var view in CardsLayer.Children.OfType<View>())
        {
            // Carte deja affichee (retour sur l'onglet) : sa vue ne bouge pas.
            if (view.BindingContext is not DashboardCardState card || !missing.Remove(card))
                free.Add(view);
        }

        // D'abord une vue du meme rendu (son tableau ou son graphique est repris), sinon n'importe laquelle.
        Reuse(missing, free, sameRenderOnly: true);
        Reuse(missing, free, sameRenderOnly: false);

        foreach (var view in free)
            ParkView(view);

        foreach (var card in missing)
        {
            if (version != _fillVersion)
                return;

            // Supprimee, ou deja affichee par le concepteur, depuis le debut du remplissage.
            if (_sourceCards?.Contains(card) != true || FindView(card) is not null)
                continue;

            using (PerfTrace.Measure($"Carte '{card.Definition.Title}' : creation de la vue"))
                CreateCardView(card);

            // Rend la main : le thread UI dessine et traite les clics entre deux cartes.
            await Task.Delay(1);
        }
    }

    private void Reuse(List<DashboardCardState> missing, List<View> free, bool sameRenderOnly)
    {
        for (var i = missing.Count - 1; i >= 0 && free.Count > 0; i--)
        {
            var card = missing[i];
            var index = sameRenderOnly ? free.FindIndex(view => HasRenderOf(view, card)) : 0;
            if (index < 0)
                continue;

            BindCard(free[index], card);
            free.RemoveAt(index);
            missing.RemoveAt(i);
        }
    }

    private bool HasRenderOf(View view, DashboardCardState card)
        => _viewKinds.TryGetValue(view, out var kind) && RenderOf(kind) == RenderOf(card.View);

    /// <summary>Rendu lourd d'une carte : 1 = tableau, 2 = graphique, 0 = aucun.</summary>
    private static int RenderOf(DashboardCardKind kind) => kind switch
    {
        DashboardCardKind.Grid => 1,
        DashboardCardKind.BarChart or DashboardCardKind.LineChart or DashboardCardKind.PieChart => 2,
        _ => 0,
    };

    private View? FindView(DashboardCardState card)
        => CardsLayer.Children.OfType<View>().FirstOrDefault(view => ReferenceEquals(view.BindingContext, card));

    private void CreateCardView(DashboardCardState card)
    {
        _cardTemplate ??= (DataTemplate)Resources["CardTemplate"];
        var view = (View)_cardTemplate.CreateContent();

        // Liee avant d'entrer dans le calque : elle n'herite jamais du contexte de la grille.
        BindCard(view, card);
        CardsLayer.Add(view);
    }

    private void BindCard(View view, DashboardCardState card)
    {
        _viewKinds[view] = card.View;
        view.BindingContext = card;
        view.IsVisible = true;
    }

    /// <summary>Vue mise en reserve : masquee et sans carte, elle servira la prochaine carte a afficher.</summary>
    private static void ParkView(View view)
    {
        view.IsVisible = false;
        view.BindingContext = null;
    }

    /// <summary>Concepteur : cartes ajoutees ou supprimees sur la page affichee.</summary>
    private void OnSourceCardsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (DashboardCardState card in e.NewItems!)
                    ShowCard(card);
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (DashboardCardState card in e.OldItems!)
                {
                    if (FindView(card) is { } view)
                        ParkView(view);
                }
                break;
            default:
                ShowCards(_sourceCards, force: true);
                break;
        }
    }

    private void ShowCard(DashboardCardState card)
    {
        if (FindView(card) is not null)
            return;

        if (CardsLayer.Children.OfType<View>().FirstOrDefault(view => view.BindingContext is not DashboardCardState) is { } parked)
            BindCard(parked, card);
        else
            CreateCardView(card);
    }

    private static void OnSelectedCardChanged(BindableObject bindable, object oldValue, object newValue)
    {
#if WINDOWS
        // Les cartes ne sont pas focalisables : le ScrollViewer natif recoit le clavier des qu'une carte est selectionnee.
        if (newValue is not null)
            ((DashboardGrid)bindable).FocusGrid();
#endif
    }

    private void OnGridScrollHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        AttachKeyboard();
#endif
    }

#if WINDOWS
    private global::Microsoft.UI.Xaml.Controls.ScrollViewer? _nativeScroll;

    private void AttachKeyboard()
    {
        if (_nativeScroll is not null)
            _nativeScroll.KeyDown -= OnNativeKeyDown;

        _nativeScroll = GridScroll.Handler?.PlatformView as global::Microsoft.UI.Xaml.Controls.ScrollViewer;
        if (_nativeScroll is null)
            return;

        _nativeScroll.IsTabStop = true;
        _nativeScroll.UseSystemFocusVisuals = false;
        _nativeScroll.KeyDown += OnNativeKeyDown;
    }

    // FocusState.Pointer : pas de cadre de focus autour de la grille.
    private void FocusGrid()
        => _nativeScroll?.Focus(global::Microsoft.UI.Xaml.FocusState.Pointer);

    private void OnNativeKeyDown(object sender, global::Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // Seulement dans le concepteur, et jamais pendant une saisie dans un champ de texte.
        if (Session is not { IsDesigning: true } || SelectedCard is not { } card
            || e.OriginalSource is global::Microsoft.UI.Xaml.Controls.TextBox)
            return;

        ICommand? command;
        object? parameter;
        switch (e.Key)
        {
            case global::Windows.System.VirtualKey.Escape:
                (command, parameter) = (SelectCardCommand, null);
                break;
            case global::Windows.System.VirtualKey.Delete:
                (command, parameter) = (DeleteCardCommand, card);
                break;
            default:
                return;
        }

        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
            e.Handled = true;
        }
    }
#endif

    /// <summary>Les calques (cases, cartes, apercu) partagent les memes colonnes et lignes.</summary>
    private void ApplyGridSize()
    {
        _cells.ColumnCount = Math.Max(1, ColumnCount);
        _cells.RowCount = Math.Max(1, RowCount);
        _cells.RowHeight = RowHeight;
        _cells.ColumnSpacing = CardsLayer.ColumnSpacing;
        _cells.RowSpacing = CardsLayer.RowSpacing;
        _cells.HoveredCell = null;
        CellsCanvas.Invalidate();

        foreach (var layer in new[] { CardsLayer, PreviewLayer })
        {
            layer.ColumnDefinitions.Clear();
            for (var i = 0; i < Math.Max(1, ColumnCount); i++)
                layer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            layer.RowDefinitions.Clear();
            for (var i = 0; i < Math.Max(1, RowCount); i++)
                layer.RowDefinitions.Add(new RowDefinition(RowHeight));
        }
    }

    // ---------- Cases libres du concepteur (canevas) ----------

    private void OnCellsTapped(object? sender, TappedEventArgs e)
    {
        if (CellAt(e.GetPosition(CellsCanvas)) is { } cell)
            Execute(AddCardCommand, cell);
    }

    private void OnCellsPointerMoved(object? sender, PointerEventArgs e) => SetHoveredCell(CellAt(e.GetPosition(CellsCanvas)));

    private void OnCellsPointerExited(object? sender, PointerEventArgs e) => SetHoveredCell(null);

    private void SetHoveredCell(DashboardCell? cell)
    {
        if (_cells.HoveredCell == cell)
            return;

        _cells.HoveredCell = cell;
        CellsCanvas.Invalidate();
    }

    // Mesure dans le repere du canevas (comme le survol) : le coin de la carte vient de sa case, pas de sa vue.
    private void OnCardDragStarting(object? sender, DragStartingEventArgs e)
    {
        _grabOffset = Point.Zero;
        if ((sender as BindableObject)?.BindingContext is not DashboardCardState card || e.GetPosition(CellsCanvas) is not { } pointer)
            return;

        var origin = _cells.GetCellOrigin(card.Column, card.Row, CellsCanvas.Width);
        _grabOffset = new Point(pointer.X - origin.X, pointer.Y - origin.Y);
    }

    private void OnCellsDragOver(object? sender, DragEventArgs e)
    {
        var cell = DropCellAt(e.GetPosition(CellsCanvas));
        if (cell == _dragCell)
            return;

        LeaveDragCell();
        _dragCell = cell;
        if (cell is not null)
            Execute(DragOverCommand, cell);
    }

    // DragLeave remonte aussi quand le pointeur passe d'un enfant a l'autre (case -> carte) : seule une vraie sortie compte.
    private void OnCellsDragLeave(object? sender, DragEventArgs e)
    {
        if (e.GetPosition(DropArea) is { } point && DropArea.Bounds.Size is var size
            && point.X >= 0 && point.Y >= 0 && point.X < size.Width && point.Y < size.Height)
            return;

        LeaveDragCell();
    }

    private void OnCellsDrop(object? sender, DropEventArgs e)
    {
        var cell = DropCellAt(e.GetPosition(CellsCanvas)) ?? _dragCell;
        _dragCell = null;
        if (cell is not null)
            Execute(DropCommand, cell);
    }

    /// <summary>Case du coin haut-gauche de la carte glissee, arrondie a la plus proche.</summary>
    private DashboardCell? DropCellAt(Point? position)
    {
        if (position is not { } point || Session is not { DraggedCard: not null })
            return null;

        return _cells.SnapCell(new Point(point.X - _grabOffset.X, point.Y - _grabOffset.Y), CellsCanvas.Width);
    }

    private void LeaveDragCell()
    {
        if (_dragCell is not { } cell)
            return;

        _dragCell = null;
        Execute(DragLeaveCommand, cell);
    }

    private DashboardCell? CellAt(Point? position)
        => position is { } point ? _cells.HitTest(point, CellsCanvas.Width) : null;

    private static void Execute(ICommand? command, object parameter)
    {
        if (command?.CanExecute(parameter) == true)
            command.Execute(parameter);
    }

    private static Color ResourceColor(string key, Color fallback)
        => Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;

    private void OnFlowchartButtonClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.FrameworkElement platformView)
            return;

        var flyout = Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.GetAttachedFlyout(platformView)
                     ?? platformView.ContextFlyout;

        flyout?.ShowAt(platformView);
    }
}
