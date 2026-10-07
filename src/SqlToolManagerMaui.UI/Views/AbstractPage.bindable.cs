using CommunityToolkit.Maui;
using SqlToolManagerMaui.UI.ViewModels;

namespace SqlToolManagerMaui.UI.Views;

public abstract partial class AbstractPage
{
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial bool IsPageFocused { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial bool TabBarIsVisible { get; set; }

    [BindableProperty]
    public partial bool IsShellRoot { get; set; }

    public static readonly BindableProperty PageRouteProperty =
        BindableProperty.Create(
            nameof(PageRoute),
            typeof(string),
            typeof(AbstractPage),
            string.Empty,
            BindingMode.OneWayToSource);

    private void InitBindings()
    {
        SetBinding(IsEnabledProperty,
            new Binding(nameof(AbstractVM.IsNotBusy),
                source: BindingContext,
                mode: BindingMode.OneWay));

        SetBinding(IsPageFocusedProperty,
            new Binding(nameof(AbstractVM.IsFocused),
                source: BindingContext,
                mode: BindingMode.TwoWay));

        SetBinding(IsShellRootProperty,
            new Binding(nameof(AbstractVM.IsShellRoot),
                source: BindingContext,
                mode: BindingMode.OneWay));

        SetBinding(Shell.TabBarIsVisibleProperty,
            new Binding(nameof(AbstractPage.TabBarIsVisible),
                source: this,
                mode: BindingMode.OneWay));
    }
}