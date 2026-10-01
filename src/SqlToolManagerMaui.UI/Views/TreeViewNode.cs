using Microsoft.Maui.Controls.Shapes;
using ShapePath = Microsoft.Maui.Controls.Shapes.Path;

namespace SqlToolManagerMaui.UI.Views;

/// <summary>
/// Noeud d'un <see cref="TreeView"/> : en-tete (chevron + ItemTemplate) puis, sous lui,
/// ses propres enfants (meme gabarit, un niveau plus loin). Interne au TreeView.
/// </summary>
public sealed class TreeViewNode : ContentView
{
    private const double ChevronSize = 16d;

    private readonly TreeView _owner;
    private readonly Border _header;
    private readonly BoxView _hover;
    private readonly BoxView _indicator;
    private readonly ShapePath _chevron;
    private readonly ContentView _chevronHost;

    internal VerticalStackLayout ChildrenHost { get; } = new();

    public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(
        nameof(IsExpanded), typeof(bool), typeof(TreeViewNode), false, BindingMode.TwoWay,
        propertyChanged: (b, _, n) => ((TreeViewNode)b).UpdateExpanded((bool)n));

    /// <summary>Etat d'ouverture, lie en TwoWay au chemin <see cref="TreeView.IsExpandedPath"/> de l'element.</summary>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public static readonly BindableProperty CanExpandProperty = BindableProperty.Create(
        nameof(CanExpand), typeof(bool), typeof(TreeViewNode), false);

    public bool CanExpand
    {
        get => (bool)GetValue(CanExpandProperty);
        set => SetValue(CanExpandProperty, value);
    }

    internal TreeViewNode(TreeView owner, int level)
    {
        _owner = owner;

        // ---------- Chevron ----------
        _chevron = new ShapePath
        {
            Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString("M 0,0 L 4,4 L 0,8")!,
            StrokeThickness = 1.2,
            StrokeLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            WidthRequest = 6,
            HeightRequest = 10,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        _chevron.SetBinding(Shape.StrokeProperty, new Binding(nameof(TreeView.ChevronColor), source: owner));

        _chevronHost = new ContentView
        {
            WidthRequest = ChevronSize,
            HeightRequest = ChevronSize,
            VerticalOptions = LayoutOptions.Center,
            Content = _chevron,
        };
        _chevronHost.SetBinding(IsVisibleProperty, new Binding(nameof(CanExpand), source: this));
        _chevronHost.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(Toggle) });

        // ---------- Contenu fourni par l'appelant ----------
        var content = owner.ItemTemplate?.CreateContent() as View ?? new Label();
        content.VerticalOptions = LayoutOptions.Center;

        var row = new Grid
        {
            ColumnDefinitions = { new(ChevronSize), new(GridLength.Star) },
            ColumnSpacing = 4,
            Padding = new Thickness(4 + level * owner.Indent, 0, 4, 0),
        };
        row.Add(_chevronHost, 0);
        row.Add(content, 1);

        // ---------- Fond de survol / selection + pastille ----------
        _hover = new BoxView { Opacity = 0, InputTransparent = true };
        _hover.SetBinding(BoxView.ColorProperty, new Binding(nameof(TreeView.ItemHoverColor), source: owner));
        _hover.SetBinding(BoxView.CornerRadiusProperty, new Binding(nameof(TreeView.ItemCornerRadius), source: owner,
            converter: new FuncConverter(r => new CornerRadius((double)r!))));

        _indicator = new BoxView
        {
            WidthRequest = 3,
            HeightRequest = 14,
            CornerRadius = 1.5,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false,
            InputTransparent = true,
        };
        _indicator.SetBinding(BoxView.ColorProperty, new Binding(nameof(TreeView.SelectionIndicatorColor), source: owner));

        _header = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            Content = new Grid { Children = { _hover, _indicator, row } },
        };
        _header.SetBinding(HeightRequestProperty, new Binding(nameof(TreeView.ItemHeight), source: owner));
        _header.SetBinding(Border.StrokeShapeProperty, new Binding(nameof(TreeView.ItemCornerRadius), source: owner,
            converter: new FuncConverter(r => new RoundRectangle { CornerRadius = (double)r! })));

        _header.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _owner.Select(this)) });
        _header.GestureRecognizers.Add(new TapGestureRecognizer { NumberOfTapsRequired = 2, Command = new Command(Toggle) });
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => _hover.Opacity = 1;
        pointer.PointerExited += (_, _) => _hover.Opacity = 0;
        _header.GestureRecognizers.Add(pointer);

        // ---------- Enfants : meme gabarit, un niveau plus loin ----------
        ChildrenHost.IsVisible = false;
        BindableLayout.SetItemTemplate(ChildrenHost, new DataTemplate(() => new TreeViewNode(owner, level + 1)));

        Content = new VerticalStackLayout { Children = { _header, ChildrenHost } };
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        // Chemins lus a la creation : ils sont declares une fois sur le TreeView.
        SetBinding(IsExpandedProperty, new Binding(_owner.IsExpandedPath, BindingMode.TwoWay));
        SetBinding(CanExpandProperty, new Binding(_owner.CanExpandPath));
        ChildrenHost.SetBinding(BindableLayout.ItemsSourceProperty, new Binding(_owner.ChildrenPath));

        _owner.Attach(this);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is null)
            _owner.Release(this);
    }

    internal void SetSelected(bool isSelected)
    {
        _header.BackgroundColor = isSelected ? _owner.ItemSelectedColor : Colors.Transparent;
        _indicator.IsVisible = isSelected;
    }

    private void Toggle()
    {
        if (!CanExpand)
            return;

        // Shift gauche + clic sur un noeud ouvert : referme aussi toute sa descendance.
        // Pas d'equivalent a l'ouverture : tout deplier chargerait trop d'elements.
        if (IsExpanded && IsLeftShiftPressed())
        {
            CollapseAll();
            return;
        }

        IsExpanded = !IsExpanded;
    }

    private void CollapseAll()
    {
        foreach (var child in ChildNodes())
            child.CollapseAll();

        IsExpanded = false;
    }

    private IEnumerable<TreeViewNode> ChildNodes() => ChildrenHost.Children.OfType<TreeViewNode>();

    private static bool IsLeftShiftPressed()
    {
#if WINDOWS
        var state = global::Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(global::Windows.System.VirtualKey.LeftShift);
        return state.HasFlag(global::Windows.UI.Core.CoreVirtualKeyStates.Down);
#else
        return false;
#endif
    }

    private void UpdateExpanded(bool isExpanded)
    {
        ChildrenHost.IsVisible = isExpanded;
        _chevron.Rotation = isExpanded ? 90 : 0;

        if (isExpanded && _owner.ExpandingCommand is { } command && command.CanExecute(BindingContext))
            command.Execute(BindingContext);
    }

    /// <summary>Convertisseur minimal pour une liaison interne.</summary>
    private sealed class FuncConverter(Func<object?, object?> convert) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => convert(value);

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException();
    }
}
