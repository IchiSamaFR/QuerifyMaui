using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls;

/// <summary>
/// Item du rail de navigation : pastille de selection, icone et libelle,
/// avec survol facon Windows 11.
/// </summary>
public partial class NavigationRailItem : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(NavigationRailItem), string.Empty);

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(Enum), typeof(NavigationRailItem), null);

    public static readonly BindableProperty RouteProperty =
        BindableProperty.Create(nameof(Route), typeof(string), typeof(NavigationRailItem), string.Empty,
            propertyChanged: OnSelectionSourceChanged);

    public static readonly BindableProperty SelectedRouteProperty =
        BindableProperty.Create(nameof(SelectedRoute), typeof(string), typeof(NavigationRailItem), string.Empty,
            propertyChanged: OnSelectionSourceChanged);

    public static readonly BindableProperty IsSelectedProperty =
        BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(NavigationRailItem), false,
            propertyChanged: (b, o, n) => ((NavigationRailItem)b).UpdateBackground());

    public static readonly BindableProperty IsLabelVisibleProperty =
        BindableProperty.Create(nameof(IsLabelVisible), typeof(bool), typeof(NavigationRailItem), true);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(NavigationRailItem), null);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Enum? Icon
    {
        get => (Enum?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Route
    {
        get => (string)GetValue(RouteProperty);
        set => SetValue(RouteProperty, value);
    }

    public string SelectedRoute
    {
        get => (string)GetValue(SelectedRouteProperty);
        set => SetValue(SelectedRouteProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsLabelVisible
    {
        get => (bool)GetValue(IsLabelVisibleProperty);
        set => SetValue(IsLabelVisibleProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private bool _isHovered;

    public NavigationRailItem()
    {
        InitializeComponent();
    }

    private static void OnSelectionSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var item = (NavigationRailItem)bindable;
        item.IsSelected = !string.IsNullOrEmpty(item.Route)
            && string.Equals(item.Route, item.SelectedRoute, StringComparison.Ordinal);
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (Command?.CanExecute(Route) == true)
            Command.Execute(Route);
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        _isHovered = true;
        UpdateBackground();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _isHovered = false;
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        var key = (IsSelected, _isHovered) switch
        {
            (true, _) => "NavItemSelectedBrush",
            (false, true) => "NavItemHoverBrush",
            _ => null
        };

        if (key is null)
        {
            Container.SetAppThemeColor(BackgroundColorProperty, Colors.Transparent, Colors.Transparent);
            return;
        }

        var light = key == "NavItemSelectedBrush" ? "NavItemSelectedLight" : "NavItemHoverLight";
        var dark = key == "NavItemSelectedBrush" ? "NavItemSelectedDark" : "NavItemHoverDark";

        if (Application.Current?.Resources.TryGetValue(light, out var lightColor) == true
            && Application.Current?.Resources.TryGetValue(dark, out var darkColor) == true)
        {
            Container.SetAppThemeColor(BackgroundColorProperty, (Color)lightColor, (Color)darkColor);
        }
    }
}
