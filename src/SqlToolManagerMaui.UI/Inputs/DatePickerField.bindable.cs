using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class DatePickerField
{
    // ── Bindable Properties ──────────────────────────────────────────────

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnDatePropertyChanged))]
    public partial System.DateTime? Date { get; set; }

    [BindableProperty]
    public partial System.DateTime MaximumDate { get; set; } = (System.DateTime)DatePicker.MaximumDateProperty.DefaultValue;

    [BindableProperty]
    public partial System.DateTime MinimumDate { get; set; } = (System.DateTime)DatePicker.MinimumDateProperty.DefaultValue;

    [BindableProperty]
    public partial string DateFormat { get; set; } = (string)DatePicker.FormatProperty.DefaultValue;

    [BindableProperty]
    public partial LayoutOptions TextHorizontalOptions { get; set; } = LayoutOptions.Center;

    [BindableProperty]
    public partial LayoutOptions TextVerticalOptions { get; set; } = LayoutOptions.Center;

    private static void OnDatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DatePickerField)bindable).OnDateChanged();
}
