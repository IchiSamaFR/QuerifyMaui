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
        BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(NavigationRailItem), false);

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
}