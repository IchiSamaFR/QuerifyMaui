namespace SqlToolManagerMaui.UI.Inputs;

public partial class TimePickerField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    public TimeSpan? Time
    {
        get => (TimeSpan?)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public static readonly BindableProperty TimeProperty = BindableProperty.Create(
        nameof(Time),
        typeof(TimeSpan?),
        typeof(TimePickerField),
        defaultValue: null,
        defaultBindingMode: BindingMode.TwoWay
    );

    public string TimeFormat
    {
        get => (string)GetValue(TimeFormatProperty);
        set => SetValue(TimeFormatProperty, value);
    }

    public static readonly BindableProperty TimeFormatProperty = BindableProperty.Create(
        nameof(TimeFormat),
        typeof(string),
        typeof(TimePickerField),
        defaultValue: TimePicker.FormatProperty.DefaultValue
    );

    public LayoutOptions TextHorizontalOptions
    {
        get => (LayoutOptions)GetValue(TextHorizontalOptionsProperty);
        set => SetValue(TextHorizontalOptionsProperty, value);
    }

    public static readonly BindableProperty TextHorizontalOptionsProperty = BindableProperty.Create(
        nameof(TextHorizontalOptions),
        typeof(LayoutOptions),
        typeof(TimePickerField),
        defaultValue: LayoutOptions.Center
    );

    public LayoutOptions TextVerticalOptions
    {
        get => (LayoutOptions)GetValue(TextVerticalOptionsProperty);
        set => SetValue(TextVerticalOptionsProperty, value);
    }

    public static readonly BindableProperty TextVerticalOptionsProperty = BindableProperty.Create(
        nameof(TextVerticalOptions),
        typeof(LayoutOptions),
        typeof(TimePickerField),
        defaultValue: LayoutOptions.Center
    );
}
