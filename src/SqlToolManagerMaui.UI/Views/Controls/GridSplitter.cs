using CommunityToolkit.Maui;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using WinUIElement = Microsoft.UI.Xaml.UIElement;

namespace SqlToolManagerMaui.UI.Views.Controls;

/// <summary>
/// Splitter a placer dans une colonne ou une ligne d'une Grid (horizontal si Width > Height, sinon vertical).
/// Vertical : redimensionne la colonne a sa gauche. Horizontal : redimensionne les vues de la ligne en dessous.
/// </summary>
public partial class GridSplitter : ContentView
{
    /// <summary>Taille minimale des zones de part et d'autre du splitter.</summary>
    [BindableProperty]
    public partial double MinimumSize { get; set; } = 100d;

    private bool _isDragging;
    private bool _horizontal;
    private double _startPointer;
    private double _startPosition;
    private double _startSize;

    private bool IsHorizontal => Width > Height;

    private const double Thickness = 2;

    public GridSplitter()
    {
        MinimumWidthRequest = Thickness;
        MinimumHeightRequest = Thickness;
        // Construit en C# : la lib UI cible Windows sans etre une Exe, le compilateur XAML WinUI
        // refuserait de compiler un .xaml MAUI (WMC0001).
        var splitter = new BoxView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
        };
        splitter.SetBinding(BoxView.ColorProperty, new Binding(nameof(BackgroundColor), source: this));
        Content = splitter;
        SizeChanged += (_, _) => { if (_nativeView is not null) SetResizeCursor(_nativeView, IsHorizontal); };
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
        if (_nativeView is null || Parent is not Grid grid)
            return;

        _horizontal = IsHorizontal;
        var size = _horizontal ? GetRowChildrenHeight(grid) : GetTargetColumn(grid) is { } column ? GetCurrentWidth(grid, column) : null;
        if (size is null)
            return;

        _startSize = size.Value;
        _startPointer = GetPointer(grid, e);
        _startPosition = _horizontal ? Y : X;
        _isDragging = _nativeView.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging || Parent is not Grid grid)
            return;

        // Position relative a la Grid parente (et non au splitter qui bouge) : pas d'effet de rebond.
        var delta = GetPointer(grid, e) - _startPointer;

        if (_horizontal)
        {
            // Zone du bas : tirer vers le haut l'agrandit.
            var max = _startSize + _startPosition - MinimumSize;
            var height = Math.Clamp(_startSize - delta, MinimumSize, Math.Max(MinimumSize, max));
            foreach (var child in GetRowChildren(grid))
                child.HeightRequest = height;
        }
        else if (GetTargetColumn(grid) is { } column)
        {
            var max = grid.Width - Width - grid.ColumnSpacing * 2 - MinimumSize;
            column.Width = new GridLength(Math.Clamp(_startSize + delta, MinimumSize, Math.Max(MinimumSize, max)));
            grid.InvalidateMeasure();
        }

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
    {
        // ProtectedCursor est protege dans WinUI : on passe par la reflexion.
        typeof(WinUIElement)
            .GetProperty("ProtectedCursor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(view, InputSystemCursor.Create(horizontal ? InputSystemCursorShape.SizeNorthSouth : InputSystemCursorShape.SizeWestEast));
    }

    private List<VisualElement> GetRowChildren(Grid grid)
    {
        var row = Grid.GetRow(this) + 1;
        return grid.Children.OfType<VisualElement>().Where(c => Grid.GetRow((BindableObject)c) == row && Grid.GetRowSpan((BindableObject)c) == 1).ToList();
    }

    private double? GetRowChildrenHeight(Grid grid)
    {
        var children = GetRowChildren(grid);
        return children.Count > 0 ? children.Max(c => c.Height) : null;
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
