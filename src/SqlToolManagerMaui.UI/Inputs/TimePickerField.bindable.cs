using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class TimePickerField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial TimeSpan? Time { get; set; }

    [BindableProperty]
    public partial string TimeFormat { get; set; } = (string)TimePicker.FormatProperty.DefaultValue;

    [BindableProperty]
    public partial LayoutOptions TextHorizontalOptions { get; set; } = LayoutOptions.Center;

    [BindableProperty]
    public partial LayoutOptions TextVerticalOptions { get; set; } = LayoutOptions.Center;
}
