namespace SqlToolManagerMaui.UI.Inputs;

public partial class DatePickerField
{
    // ── Bindable Properties ──────────────────────────────────────────────

    public System.DateTime? Date
    {
        get => (System.DateTime?)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public static readonly BindableProperty DateProperty = BindableProperty.Create(
        nameof(Date),
        typeof(System.DateTime?),
        typeof(DatePickerField),
        defaultValue: null,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, _, _) => ((DatePickerField)bindable).OnDateChanged()
    );

    public System.DateTime MaximumDate
    {
        get => (System.DateTime)GetValue(MaximumDateProperty);
        set => SetValue(MaximumDateProperty, value);
    }

    public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(
        nameof(MaximumDate),
        typeof(System.DateTime),
        typeof(DatePickerField),
        defaultValue: DatePicker.MaximumDateProperty.DefaultValue
    );

    public System.DateTime MinimumDate
    {
        get => (System.DateTime)GetValue(MinimumDateProperty);
        set => SetValue(MinimumDateProperty, value);
    }

    public static readonly BindableProperty MinimumDateProperty = BindableProperty.Create(
        nameof(MinimumDate),
        typeof(System.DateTime),
        typeof(DatePickerField),
        defaultValue: DatePicker.MinimumDateProperty.DefaultValue
    );

    public string DateFormat
    {
        get => (string)GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    public static readonly BindableProperty DateFormatProperty = BindableProperty.Create(
        nameof(DateFormat),
        typeof(string),
        typeof(DatePickerField),
        defaultValue: DatePicker.FormatProperty.DefaultValue
    );

    public LayoutOptions TextHorizontalOptions
    {
        get => (LayoutOptions)GetValue(TextHorizontalOptionsProperty);
        set => SetValue(TextHorizontalOptionsProperty, value);
    }

    public static readonly BindableProperty TextHorizontalOptionsProperty = BindableProperty.Create(
        nameof(TextHorizontalOptions),
        typeof(LayoutOptions),
        typeof(DatePickerField),
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
        typeof(DatePickerField),
        defaultValue: LayoutOptions.Center
    );
}
