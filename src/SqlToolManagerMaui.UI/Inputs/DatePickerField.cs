using SqlToolManagerMaui.UI.Inputs.Core;

namespace SqlToolManagerMaui.UI.Inputs;

/// <summary>
/// Champ DatePicker avec titre flottant, icône et validations.
/// Réimplémentation standalone (sans UraniumUI / Plainer).
/// </summary>
[ContentProperty(nameof(Validations))]
public partial class DatePickerField : InputField
{
    private bool _isSyncing;

    private readonly BorderlessDatePicker _datePickerView = new BorderlessDatePicker
    {
        Margin = new Thickness(0),
        Opacity = 1,
    };

    public BorderlessDatePicker DatePickerView => _datePickerView;

    public override View Content { get; set; }
    public Grid GridContent { get; set; } = new Grid
    {
        Margin = 0,
        ColumnDefinitions =
        [
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Star),
        ],
    };

    public override bool HasValue => true;

    public DatePickerField()
    {
        GridContent.Add(_datePickerView, column: 1);

        Content = GridContent;
        RegisterForEvents();

        OnIconChanged();
        BindPickerViews();
    }

    private void BindPickerViews()
    {
        _datePickerView.SetBinding(DatePicker.IsEnabledProperty, new Binding(nameof(IsEnabled), source: this));
        _datePickerView.SetBinding(DatePicker.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        _datePickerView.SetBinding(DatePicker.FontAutoScalingEnabledProperty, new Binding(nameof(FontAutoScalingEnabled), source: this));
        _datePickerView.SetBinding(DatePicker.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
        _datePickerView.SetBinding(DatePicker.FontAttributesProperty, new Binding(nameof(FontAttributes), source: this));
        _datePickerView.SetBinding(DatePicker.HorizontalOptionsProperty, new Binding(nameof(TextHorizontalOptions), source: this));
        _datePickerView.SetBinding(DatePicker.VerticalOptionsProperty, new Binding(nameof(TextVerticalOptions), source: this));
        _datePickerView.SetBinding(DatePicker.MinimumDateProperty, new Binding(nameof(MinimumDate), source: this));
        _datePickerView.SetBinding(DatePicker.MaximumDateProperty, new Binding(nameof(MaximumDate), source: this));
        _datePickerView.SetBinding(DatePicker.FormatProperty, new Binding(nameof(DateFormat), source: this));
        _datePickerView.SetBinding(DatePicker.TextColorProperty, new Binding(nameof(TextColor), source: this));

        _datePickerView.DateSelected += OnDatePickerDateSelected;
    }

    private void OnDatePickerDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (_isSyncing)
            return;

        _isSyncing = true;
        Date = e.NewDate ?? System.DateTime.Today;
        _isSyncing = false;

        OnDateUpdated();
    }

    protected override object? GetValueForValidator() => Date;

    protected virtual void OnDateChanged()
    {
        if (_isSyncing)
            return;

        _isSyncing = true;

        if (Date.HasValue)
        {
            _datePickerView.Date = Date.Value.Date;
            _datePickerView.Opacity = 1;
        }
        else
        {
            _datePickerView.Opacity = 0;
        }

        _isSyncing = false;

        OnDateUpdated();
    }

    private void OnDateUpdated()
    {
        OnPropertyChanged(nameof(Date));
        CheckAndShowValidations();

        if (Date.HasValue)
            _datePickerView.Opacity = 1;

        UpdateState();
    }

    protected override void OnIconChanged()
    {
        if (Icon is not null)
        {
            imageIcon.Value.Source = Icon;
            ApplyIconColor(Icon);

            if (!GridContent.Contains(imageIcon.Value))
                GridContent.Add(imageIcon.Value, column: 0);
        }
    }

    private void ApplyIconColor(ImageSource source)
    {
        if (source is not FontImageSource font)
            return;

        if (font.Color is null || font.Color == Colors.Transparent)
        {
            font.SetAppThemeColor(
                FontImageSource.ColorProperty,
                InputColors.Get("OnBackground", Colors.Gray),
                InputColors.Get("OnBackgroundDark", Colors.Gray));
        }
    }

    public override void ResetValidation()
    {
        Date = System.DateTime.Now;
        base.ResetValidation();
    }
}
