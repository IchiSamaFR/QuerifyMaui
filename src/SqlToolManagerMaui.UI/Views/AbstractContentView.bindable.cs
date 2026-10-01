using SqlToolManagerMaui.UI.ViewModels;

namespace SqlToolManagerMaui.UI.Views;

public abstract partial class AbstractContentView
{
    public static readonly BindableProperty IsViewFocusedProperty =
        BindableProperty.Create(
            nameof(IsViewFocused),
            typeof(bool),
            typeof(AbstractContentView),
            false,
            BindingMode.TwoWay);

    public bool IsViewFocused
    {
        get { return (bool)GetValue(IsViewFocusedProperty); }
        set { SetValue(IsViewFocusedProperty, value); }
    }

    private void InitBindings()
    {
        SetBinding(IsEnabledProperty,
            new Binding(nameof(AbstractVM.IsNotBusy),
                source: BindingContext,
                mode: BindingMode.OneWay));

        SetBinding(IsViewFocusedProperty,
            new Binding(nameof(AbstractVM.IsFocused),
                source: BindingContext,
                mode: BindingMode.TwoWay));
    }
}