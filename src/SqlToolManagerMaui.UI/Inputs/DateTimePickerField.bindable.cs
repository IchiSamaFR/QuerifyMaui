using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.UI.Inputs;

public partial class DateTimePickerField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    [BindableProperty(PropertyChangedMethodName = nameof(OnIconPropertyChanged))]
    public partial ImageSource? SecondIcon { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnDateTimePropertyChanged))]
    public partial System.DateTime DateTime { get; set; } = System.DateTime.Now;

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnDateTimeOffsetPropertyChanged))]
    public partial System.DateTimeOffset DateTimeOffset { get; set; } = System.DateTimeOffset.Now;

    [BindableProperty(PropertyChangedMethodName = nameof(OnRangePropertyChanged))]
    public partial System.DateTime MaximumDate { get; set; } = (System.DateTime)DatePicker.MaximumDateProperty.DefaultValue;

    [BindableProperty(PropertyChangedMethodName = nameof(OnRangePropertyChanged))]
    public partial System.DateTime MinimumDate { get; set; } = (System.DateTime)DatePicker.MinimumDateProperty.DefaultValue;

    [BindableProperty]
    public partial string DateFormat { get; set; } = (string)DatePicker.FormatProperty.DefaultValue;

    [BindableProperty]
    public partial string TimeFormat { get; set; } = (string)TimePicker.FormatProperty.DefaultValue;

    [BindableProperty]
    public partial LayoutOptions TextHorizontalOptions { get; set; } = LayoutOptions.Fill;

    [BindableProperty]
    public partial LayoutOptions TextVerticalOptions { get; set; } = LayoutOptions.Center;

    [BindableProperty(PropertyChangedMethodName = nameof(OnIconPropertyChanged))]
    public partial bool SecondIconIsVisible { get; set; } = true;

    private static void OnIconPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DateTimePickerField)bindable).OnIconChanged();

    private static void OnDateTimePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DateTimePickerField)bindable).OnDateTimeChanged((System.DateTime)newValue);

    private static void OnDateTimeOffsetPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DateTimePickerField)bindable).OnDateTimeOffsetChanged((System.DateTimeOffset)newValue);

    private static void OnRangePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DateTimePickerField)bindable).OnRangeChanged();
}
