using CommunityToolkit.Maui;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using SqlToolManagerMaui.UI.Extensions;
using WinUIElement = Microsoft.UI.Xaml.UIElement;

namespace SqlToolManagerMaui.UI.Views.Controls.Layouts;

/// <summary>
/// Poignees de redimensionnement d'un element place dans une Grid a cases regulieres :
/// tirer un bord ou un coin modifie sa position et son etendue, case par case.
/// </summary>
public partial class GridSpanResizer : Grid
{
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial int Column { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial int Row { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial int ColumnSpan { get; set; } = 1;

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial int RowSpan { get; set; } = 1;

    /// <summary>Nombre de colonnes de la grille : borne le bord droit (les lignes ne sont pas bornees).</summary>
    [BindableProperty]
    public partial int ColumnCount { get; set; } = int.MaxValue;

    [BindableProperty]
    public partial double ColumnSpacing { get; set; }

    [BindableProperty]
    public partial double RowSpacing { get; set; }

    // Sens tire par chaque poignee : -1 = gauche/haut, 1 = droite/bas, 0 = axe inchange.
    private readonly Dictionary<VisualElement, (int X, int Y)> _directions;
    private readonly Dictionary<WinUIElement, (int X, int Y)> _nativeHandles = [];

    // Memes instances a l'ajout et au retrait : WinUI ne retire pas un delegue recree.
    private readonly PointerEventHandler _pressed, _moved, _released, _captureLost;

    private (int X, int Y) _direction;
    private Windows.Foundation.Point _startPointer;
    private int _startColumn, _startRow, _startColumnSpan, _startRowSpan;
    private double _columnStep, _rowStep;
    private bool _isResizing;

    public GridSpanResizer()
    {
        InitializeComponent();

        _pressed = OnPointerPressed;
        _moved = OnPointerMoved;
        _released = OnPointerReleased;
        _captureLost = OnPointerCaptureLost;
        _directions = new()
        {
            [LeftHandle] = (-1, 0),
            [RightHandle] = (1, 0),
            [TopHandle] = (0, -1),
            [BottomHandle] = (0, 1),
            [TopLeftHandle] = (-1, -1),
            [TopRightHandle] = (1, -1),
            [BottomLeftHandle] = (-1, 1),
            [BottomRightHandle] = (1, 1),
        };
    }

    private void OnHandleHandlerChanging(object? sender, HandlerChangingEventArgs e)
    {
        if (e.OldHandler?.PlatformView is not WinUIElement view || !_nativeHandles.Remove(view))
            return;

        view.RemoveHandler(WinUIElement.PointerPressedEvent, _pressed);
        view.RemoveHandler(WinUIElement.PointerMovedEvent, _moved);
        view.RemoveHandler(WinUIElement.PointerReleasedEvent, _released);
        view.RemoveHandler(WinUIElement.PointerCaptureLostEvent, _captureLost);
    }

    private void OnHandleHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement { Handler.PlatformView: WinUIElement view } handle || !_directions.TryGetValue(handle, out var direction))
            return;

        _nativeHandles[view] = direction;

        // Sans Background, un Panel WinUI n'est pas hit-testable : aucun clic ne remonte.
        if (view is Microsoft.UI.Xaml.Controls.Panel panel && panel.Background is null)
            panel.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        // handledEventsToo : MAUI peut deja marquer les evenements pointeur comme traites.
        view.AddHandler(WinUIElement.PointerPressedEvent, _pressed, true);
        view.AddHandler(WinUIElement.PointerMovedEvent, _moved, true);
        view.AddHandler(WinUIElement.PointerReleasedEvent, _released, true);
        view.AddHandler(WinUIElement.PointerCaptureLostEvent, _captureLost, true);
        view.SetCursor(direction switch
        {
            (_, 0) => InputSystemCursorShape.SizeWestEast,
            (0, _) => InputSystemCursorShape.SizeNorthSouth,
            (-1, -1) or (1, 1) => InputSystemCursorShape.SizeNorthwestSoutheast,
            _ => InputSystemCursorShape.SizeNortheastSouthwest,
        });
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not WinUIElement view || !_nativeHandles.TryGetValue(view, out var direction) || ColumnSpan < 1 || RowSpan < 1)
            return;

        // L'element couvre exactement ses cases : (taille + espacement) / etendue = pas d'une case.
        _columnStep = (Width + ColumnSpacing) / ColumnSpan;
        _rowStep = (Height + RowSpacing) / RowSpan;
        if (_columnStep <= 0 || _rowStep <= 0)
            return;

        _direction = direction;
        _startColumn = Column;
        _startRow = Row;
        _startColumnSpan = ColumnSpan;
        _startRowSpan = RowSpan;
        // Position dans la fenetre (et non dans l'element qui se deplace) : pas d'effet de rebond.
        _startPointer = e.GetCurrentPoint(null).Position;
        _isResizing = view.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizing)
            return;

        var position = e.GetCurrentPoint(null).Position;
        var columns = (int)Math.Round((position.X - _startPointer.X) / _columnStep);
        var rows = (int)Math.Round((position.Y - _startPointer.Y) / _rowStep);

        if (_direction.X < 0)
        {
            var right = _startColumn + _startColumnSpan;
            var column = Math.Clamp(_startColumn + columns, 0, right - 1);
            Column = column;
            ColumnSpan = right - column;
        }
        else if (_direction.X > 0)
        {
            ColumnSpan = Math.Clamp(_startColumnSpan + columns, 1, Math.Max(1, ColumnCount - _startColumn));
        }

        if (_direction.Y < 0)
        {
            var bottom = _startRow + _startRowSpan;
            var row = Math.Clamp(_startRow + rows, 0, bottom - 1);
            Row = row;
            RowSpan = bottom - row;
        }
        else if (_direction.Y > 0)
        {
            RowSpan = Math.Max(1, _startRowSpan + rows);
        }

        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is WinUIElement view)
            view.ReleasePointerCapture(e.Pointer);

        _isResizing = false;
        e.Handled = true;
    }

    private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e) => _isResizing = false;
}
