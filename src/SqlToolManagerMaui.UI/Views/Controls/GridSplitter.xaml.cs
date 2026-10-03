using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUIElement = Microsoft.UI.Xaml.UIElement;

namespace SqlToolManagerMaui.UI.Views.Controls;

/// <summary>
/// Splitter vertical a placer dans une colonne Auto d'une Grid :
/// redimensionne la colonne situee a sa gauche en temps reel pendant le drag.
/// </summary>
public partial class GridSplitter : ContentView
{
    public static readonly BindableProperty MinimumSizeProperty =
        BindableProperty.Create(nameof(MinimumSize), typeof(double), typeof(GridSplitter), 100d);

    /// <summary>Largeur minimale des colonnes de part et d'autre du splitter.</summary>
    public double MinimumSize
    {
        get => (double)GetValue(MinimumSizeProperty);
        set => SetValue(MinimumSizeProperty, value);
    }

    private bool _isDragging;
    private double _startPointerX;
    private double _startWidth;

    public GridSplitter()
    {
        InitializeComponent();
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
        SetResizeCursor(view);
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
        if (_nativeView is null || Parent is not Grid grid || GetTargetColumn(grid) is not { } column)
            return;

        _startPointerX = GetPointerX(grid, e);
        _startWidth = GetCurrentWidth(grid, column);
        _isDragging = _nativeView.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging || Parent is not Grid grid || GetTargetColumn(grid) is not { } column)
            return;

        // Position relative a la Grid parente (et non au splitter qui bouge) : pas d'effet de rebond.
        var delta = GetPointerX(grid, e) - _startPointerX;
        var max = grid.Width - Width - grid.ColumnSpacing * 2 - MinimumSize;
        var newWidth = Math.Clamp(_startWidth + delta, MinimumSize, Math.Max(MinimumSize, max));

        column.Width = new GridLength(newWidth);
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

    private static double GetPointerX(Grid grid, PointerRoutedEventArgs e)
    {
        var reference = grid.Handler?.PlatformView as WinUIElement;
        return e.GetCurrentPoint(reference).Position.X;
    }

    private static void SetResizeCursor(WinUIElement view)
    {
        // ProtectedCursor est protege dans WinUI : on passe par la reflexion.
        typeof(WinUIElement)
            .GetProperty("ProtectedCursor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(view, InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
    }

    private ColumnDefinition? GetTargetColumn(Grid grid)
    {
        var index = Grid.GetColumn(this) - 1;
        return index >= 0 && index < grid.ColumnDefinitions.Count ? grid.ColumnDefinitions[index] : null;
    }

    private double GetCurrentWidth(Grid grid, ColumnDefinition column)
    {
        if (column.Width.IsAbsolute)
            return column.Width.Value;

        // Colonne Auto/* : largeur reelle = position du splitter - colonnes precedentes.
        var index = grid.ColumnDefinitions.IndexOf(column);
        var before = grid.ColumnDefinitions.Take(index)
                         .Sum(c => c.Width.IsAbsolute ? c.Width.Value + grid.ColumnSpacing : 0);
        return X - grid.ColumnSpacing - before;
    }
}
