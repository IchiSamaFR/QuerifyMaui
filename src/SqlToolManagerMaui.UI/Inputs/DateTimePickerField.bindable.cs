namespace SqlToolManagerMaui.UI.Inputs;

public partial class DateTimePickerField
{
    // ── Bindable Properties ───────────────────────────────────────────────────

    public ImageSource? SecondIcon
    {
        get => (ImageSource?)GetValue(SecondIconProperty);
        set => SetValue(SecondIconProperty, value);
    }

    public static readonly BindableProperty SecondIconProperty = BindableProperty.Create(
        nameof(SecondIcon),
        typeof(ImageSource),
        typeof(DateTimePickerField),
        propertyChanged: (bindable, _, _) => ((DateTimePickerField)bindable).OnIconChanged());

    public System.DateTime DateTime
    {
        get => (System.DateTime)GetValue(DateTimeProperty);
        set => SetValue(DateTimeProperty, value);
    }

    public static readonly BindableProperty DateTimeProperty = BindableProperty.Create(
        nameof(DateTime),
        typeof(System.DateTime),
        typeof(DateTimePickerField),
        defaultValue: System.DateTime.Now,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, _, newValue) => ((DateTimePickerField)bindable).OnDateTimeChanged((DateTime)newValue)
    );

    public System.DateTimeOffset DateTimeOffset
    {
        get => (System.DateTimeOffset)GetValue(DateTimeOffsetProperty);
        set => SetValue(DateTimeOffsetProperty, value);
    }

    public static readonly BindableProperty DateTimeOffsetProperty = BindableProperty.Create(
        nameof(DateTimeOffset),
        typeof(System.DateTimeOffset),
        typeof(DateTimePickerField),
        defaultValue: System.DateTimeOffset.Now,
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: (bindable, _, newValue) =>
            ((DateTimePickerField)bindable).OnDateTimeOffsetChanged((System.DateTimeOffset)newValue)
    );

    public System.DateTime MaximumDate
    {
        get => (System.DateTime)GetValue(MaximumDateProperty);
        set => SetValue(MaximumDateProperty, value);
    }

    public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(
        nameof(MaximumDate),
        typeof(System.DateTime),
        typeof(DateTimePickerField),
        defaultValue: DatePicker.MaximumDateProperty.DefaultValue,
        propertyChanged: (bindable, _, _) => ((DateTimePickerField)bindable).OnRangeChanged()
    );

    public System.DateTime MinimumDate
    {
        get => (System.DateTime)GetValue(MinimumDateProperty);
        set => SetValue(MinimumDateProperty, value);
    }

    public static readonly BindableProperty MinimumDateProperty = BindableProperty.Create(
        nameof(MinimumDate),
        typeof(System.DateTime),
        typeof(DateTimePickerField),
        defaultValue: DatePicker.MinimumDateProperty.DefaultValue,
        propertyChanged: (bindable, _, _) => ((DateTimePickerField)bindable).OnRangeChanged()
    );

    public string DateFormat
    {
        get => (string)GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    public static readonly BindableProperty DateFormatProperty = BindableProperty.Create(
        nameof(DateFormat),
        typeof(string),
        typeof(DateTimePickerField),
        defaultValue: DatePicker.FormatProperty.DefaultValue
    );

    public string TimeFormat
    {
        get => (string)GetValue(TimeFormatProperty);
        set => SetValue(TimeFormatProperty, value);
    }

    public static readonly BindableProperty TimeFormatProperty = BindableProperty.Create(
        nameof(TimeFormat),
        typeof(string),
        typeof(DateTimePickerField),
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
        typeof(DateTimePickerField),
        defaultValue: LayoutOptions.Fill
    );

    public LayoutOptions TextVerticalOptions
    {
        get => (LayoutOptions)GetValue(TextVerticalOptionsProperty);
        set => SetValue(TextVerticalOptionsProperty, value);
    }

    public static readonly BindableProperty TextVerticalOptionsProperty = BindableProperty.Create(
        nameof(TextVerticalOptions),
        typeof(LayoutOptions),
        typeof(DateTimePickerField),
        defaultValue: LayoutOptions.Center
    );

    public bool SecondIconIsVisible
    {
        get => (bool)GetValue(SecondIconIsVisibleProperty);
        set => SetValue(SecondIconIsVisibleProperty, value);
    }

    public static readonly BindableProperty SecondIconIsVisibleProperty = BindableProperty.Create(
        nameof(SecondIconIsVisible),
        typeof(bool),
        typeof(DateTimePickerField),
        defaultValue: true,
        propertyChanged: (bindable, _, _) => ((DateTimePickerField)bindable).OnIconChanged());
}
