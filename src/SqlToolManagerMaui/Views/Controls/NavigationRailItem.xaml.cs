using CommunityToolkit.Maui;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls;

/// <summary>
/// Item du rail de navigation : pastille de selection, icone et libelle,
/// avec survol facon Windows 11.
/// </summary>
public partial class NavigationRailItem : ContentView
{
    [BindableProperty]
    public partial string Text { get; set; } = string.Empty;

    [BindableProperty]
    public partial Enum? Icon { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectionSourceChanged))]
    public partial string Route { get; set; } = string.Empty;

    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectionSourceChanged))]
    public partial string SelectedRoute { get; set; } = string.Empty;

    [BindableProperty]
    public partial bool IsSelected { get; set; }

    [BindableProperty]
    public partial bool IsLabelVisible { get; set; } = true;

    [BindableProperty]
    public partial ICommand? Command { get; set; }

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