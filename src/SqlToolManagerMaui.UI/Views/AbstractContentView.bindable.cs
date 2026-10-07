using CommunityToolkit.Maui;
using SqlToolManagerMaui.UI.ViewModels;

namespace SqlToolManagerMaui.UI.Views;

public abstract partial class AbstractContentView
{
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial bool IsViewFocused { get; set; }

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