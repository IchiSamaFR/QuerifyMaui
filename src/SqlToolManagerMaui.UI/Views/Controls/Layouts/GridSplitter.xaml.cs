using CommunityToolkit.Maui;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using SqlToolManagerMaui.UI.Extensions;
using WinUIElement = Microsoft.UI.Xaml.UIElement;

namespace SqlToolManagerMaui.UI.Views.Controls.Layouts;

/// <summary>
/// Splitter a placer dans une ligne ou une colonne Auto d'une Grid (horizontal si Width > Height, sinon vertical).
/// Redimensionne la voisine non proportionnelle (celle d'avant en priorite) ; si les deux voisines sont en *, repartit leurs poids.
/// </summary>
public partial class GridSplitter : ContentView
{
    /// <summary>Taille minimale des zones de part et d'autre du splitter.</summary>
    [BindableProperty]
    public partial double MinimumSize { get; set; } = 100d;

    private bool _isDragging;
    private bool _horizontal;
    private bool _targetIsBefore;
    private int _target;
    private int? _opposite;
    private GridLength _targetLength, _oppositeLength;
    private double _startPointer;
    private double _startTarget;
    private double? _startOpposite;

    private bool IsHorizontal => Width > Height;

    public GridSplitter()
    {
        InitializeComponent();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (_nativeView is not null)
            SetResizeCursor(_nativeView, IsHorizontal);
    }

    private WinUIElement? _nativeView;
    private PointerEventHandler? _pressed, _moved, _released, _captureLost;

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        Detach();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler?.PlatformView is not WinUIElement view)
            return;

        _nativeView = view;

        // Sans Background, un Panel WinUI n'est pas hit-testable : aucun clic ne remonte.
        if (view is Microsoft.UI.Xaml.Controls.Panel panel && panel.Background is null)
            panel.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        // handledEventsToo : MAUI peut deja marquer les evenements pointeur comme traites.
        view.AddHandler(WinUIElement.PointerPressedEvent, _pressed = OnPointerPressed, true);
        view.AddHandler(WinUIElement.PointerMovedEvent, _moved = OnPointerMoved, true);
        view.AddHandler(WinUIElement.PointerReleasedEvent, _released = OnPointerReleased, true);
        view.AddHandler(WinUIElement.PointerCaptureLostEvent, _captureLost = OnPointerCaptureLost, true);
        SetResizeCursor(view, IsHorizontal);
    }

    private void Detach()
    {
        if (_nativeView is null)
            return;

        _nativeView.RemoveHandler(WinUIElement.PointerPressedEvent, _pressed!);
        _nativeView.RemoveHandler(WinUIElement.PointerMovedEvent, _moved!);
        _nativeView.RemoveHandler(WinUIElement.PointerReleasedEvent, _released!);
        _nativeView.RemoveHandler(WinUIElement.PointerCaptureLostEvent, _captureLost!);
        _nativeView = null;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_nativeView is null || Parent is not Grid grid || !TryStart(grid))
            return;

        _startPointer = GetPointer(grid, e);
        _isDragging = _nativeView.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private bool TryStart(Grid grid)
    {
        _horizontal = IsHorizontal;
        var index = _horizontal ? Grid.GetRow(this) : Grid.GetColumn(this);
        var count = GetDefinitionCount(grid);
        int? before = index > 0 && index - 1 < count ? index - 1 : null;
        int? after = index + 1 < count ? index + 1 : null;

        var target = before is { } b && !GetLength(grid, b).IsStar ? before
                   : after is { } a && !GetLength(grid, a).IsStar ? after
                   : before ?? after;
        if (target is null || GetActualSize(grid, target.Value) is not { } startTarget)
            return false;

        _target = target.Value;
        _targetIsBefore = target == before;
        _opposite = _targetIsBefore ? after : before;
        _targetLength = GetLength(grid, _target);
        _oppositeLength = _opposite is { } o ? GetLength(grid, o) : GridLength.Auto;
        _startTarget = startTarget;
        _startOpposite = _opposite is { } op ? GetActualSize(grid, op) : null;

        // Cible en * : seule une repartition avec une voisine en * mesurable est possible.
        return !_targetLength.IsStar
            || (_oppositeLength.IsStar && _startOpposite is { } so && _startTarget + so > 0);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging || Parent is not Grid grid || _target >= GetDefinitionCount(grid))
            return;

        // Position relative a la Grid parente (et non au splitter qui bouge) : pas d'effet de rebond.
        var delta = GetPointer(grid, e) - _startPointer;
        if (!_targetIsBefore)
            delta = -delta;

        var max = _startOpposite is { } opposite ? _startTarget + opposite - MinimumSize : double.PositiveInfinity;
        var size = Math.Clamp(_startTarget + delta, MinimumSize, Math.Max(MinimumSize, max));

        if (_targetLength.IsStar)
        {
            // Somme des poids conservee : les autres definitions en * ne bougent pas.
            var total = _startTarget + _startOpposite!.Value;
            var unit = (_targetLength.Value + _oppositeLength.Value) / total;
            SetLength(grid, _target, new GridLength(size * unit, GridUnitType.Star));
            SetLength(grid, _opposite!.Value, new GridLength((total - size) * unit, GridUnitType.Star));
        }
        else if (_targetLength.IsAbsolute)
        {
            SetLength(grid, _target, new GridLength(size));
        }
        else
        {
            // Auto : la taille vient du contenu, une definition fixe resterait figee une fois le contenu masque.
            foreach (var child in GetChildren(grid, _target))
            {
                if (_horizontal)
                    child.HeightRequest = size;
                else
                    child.WidthRequest = size;
            }
        }

        grid.InvalidateMeasure();
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _nativeView?.ReleasePointerCapture(e.Pointer);
        _isDragging = false;
        e.Handled = true;
    }

    private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e) => _isDragging = false;

    private double GetPointer(Grid grid, PointerRoutedEventArgs e)
    {
        var position = e.GetCurrentPoint(grid.Handler?.PlatformView as WinUIElement).Position;
        return _horizontal ? position.Y : position.X;
    }

    private static void SetResizeCursor(WinUIElement view, bool horizontal)
        => view.SetCursor(horizontal ? InputSystemCursorShape.SizeNorthSouth : InputSystemCursorShape.SizeWestEast);

    private int GetDefinitionCount(Grid grid)
        => _horizontal ? grid.RowDefinitions.Count : grid.ColumnDefinitions.Count;

    private GridLength GetLength(Grid grid, int index)
        => _horizontal ? grid.RowDefinitions[index].Height : grid.ColumnDefinitions[index].Width;

    private void SetLength(Grid grid, int index, GridLength length)
    {
        if (_horizontal)
            grid.RowDefinitions[index].Height = length;
        else
            grid.ColumnDefinitions[index].Width = length;
    }

    // MAUI n'expose pas la taille reelle d'une definition : on la lit sur son contenu.
    private double? GetActualSize(Grid grid, int index)
    {
        var length = GetLength(grid, index);
        if (length.IsAbsolute)
            return length.Value;

        var children = GetChildren(grid, index);
        return children.Count > 0 ? children.Max(c => _horizontal ? c.Height : c.Width) : null;
    }

    private List<VisualElement> GetChildren(Grid grid, int index)
        => grid.Children.OfType<VisualElement>()
               .Where(c => c != this && c.IsVisible && (_horizontal
                   ? Grid.GetRow(c) == index && Grid.GetRowSpan(c) == 1
                   : Grid.GetColumn(c) == index && Grid.GetColumnSpan(c) == 1))
               .ToList();
}
