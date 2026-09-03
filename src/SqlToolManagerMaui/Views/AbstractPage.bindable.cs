using SqlToolManagerMaui.ViewModels;

namespace SqlToolManagerMaui.Views;

public abstract partial class AbstractPage
{
    public static readonly BindableProperty IsPageFocusedProperty =
        BindableProperty.Create(
            nameof(IsPageFocused),
            typeof(bool),
            typeof(AbstractPage),
            false,
            BindingMode.TwoWay);

    public bool IsPageFocused
    {
        get { return (bool)GetValue(IsPageFocusedProperty); }
        set { SetValue(IsPageFocusedProperty, value); }
    }

    public static readonly BindableProperty TabBarIsVisibleProperty =
        BindableProperty.Create(
            nameof(TabBarIsVisible),
            typeof(bool),
            typeof(AbstractPage),
            false,
            BindingMode.TwoWay);

    public bool TabBarIsVisible
    {
        get { return (bool)GetValue(TabBarIsVisibleProperty); }
        set { SetValue(TabBarIsVisibleProperty, value); }
    }

    public static readonly BindableProperty IsShellRootProperty =
        BindableProperty.Create(
            nameof(IsShellRoot),
            typeof(bool),
            typeof(AbstractPage),
            false,
            BindingMode.Default);

    public bool IsShellRoot
    {
        get { return (bool)GetValue(IsShellRootProperty); }
        set { SetValue(IsShellRootProperty, value); }
    }

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