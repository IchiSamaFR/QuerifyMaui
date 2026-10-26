using CommunityToolkit.Maui;
using Querify.Core.Enums.Dashboards;
using Querify.Core.Models.Dashboards;
using Querify.Core.Models.Editors.Highlighting;
using Querify.Core.Models.Settings;
using Querify.Databases.Models.Connections;
using System.Windows.Input;

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

    [BindableProperty]
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
