using SqlToolManagerMaui.UI.Inputs.Core;
using System.ComponentModel;

namespace SqlToolManagerMaui.UI.Inputs;

/// <summary>
/// Champ combiné DatePicker + TimePicker avec titre flottant, icônes et validations.
/// Réimplémentation standalone (sans UraniumUI / Plainer).
/// </summary>
[ContentProperty(nameof(Validations))]
public partial class DateTimePickerField : InputField
{
    private bool _isSyncing;

    private readonly BorderlessDatePicker _datePickerView = new BorderlessDatePicker
    {
        Margin = new Thickness(0),
        Opacity = 1,
    };

    private readonly BorderlessTimePicker _timePickerView = new BorderlessTimePicker
    {
        Margin = new Thickness(0),
        Opacity = 1,
    };

    public BorderlessDatePicker DatePickerView => _datePickerView;
    public BorderlessTimePicker TimePickerView => _timePickerView;

    protected Lazy<Image> secondImageIcon = new(() => new Image
    {
        StyleClass = new[] { "InputField.Icon" },
        HorizontalOptions = LayoutOptions.Start,
        VerticalOptions = LayoutOptions.Center,
        WidthRequest = 20,
        HeightRequest = 20,
        Margin = new Thickness(8, 0, 0, 0),
    });

    public override View Content { get; set; }
    public Grid GridContent { get; set; } = new Grid
    {
        Margin = 0,
        ColumnDefinitions =
        [
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Star),
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Star),
        ],
    };

    public override bool HasValue => true;

    public DateTimePickerField()
    {
        GridContent.Add(_datePickerView, column: 1);
        GridContent.Add(_timePickerView, column: 3);

        Content = GridContent;
        RegisterForEvents();

        OnIconChanged();
        BindPickerViews();
        ApplyRangeToPickers();
    }

    private void BindPickerViews()
    {
        _datePickerView.SetBinding(DatePicker.IsEnabledProperty, new Binding(nameof(IsEnabled), source: this));
        _datePickerView.SetBinding(DatePicker.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        _datePickerView.SetBinding(DatePicker.FontAutoScalingEnabledProperty, new Binding(nameof(FontAutoScalingEnabled), source: this));
        _datePickerView.SetBinding(DatePicker.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
        _datePickerView.SetBinding(DatePicker.FontAttributesProperty, new Binding(nameof(FontAttributes), source: this));
        _datePickerView.SetBinding(DatePicker.FormatProperty, new Binding(nameof(DateFormat), source: this));
        _datePickerView.SetBinding(DatePicker.TextColorProperty, new Binding(nameof(TextColor), source: this));
        _datePickerView.SetBinding(DatePicker.HorizontalOptionsProperty, new Binding(nameof(TextHorizontalOptions), source: this));
        _datePickerView.SetBinding(DatePicker.VerticalOptionsProperty, new Binding(nameof(TextVerticalOptions), source: this));

        _timePickerView.SetBinding(TimePicker.IsEnabledProperty, new Binding(nameof(IsEnabled), source: this));
        _timePickerView.SetBinding(TimePicker.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        _timePickerView.SetBinding(TimePicker.FontAutoScalingEnabledProperty, new Binding(nameof(FontAutoScalingEnabled), source: this));
        _timePickerView.SetBinding(TimePicker.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
        _timePickerView.SetBinding(TimePicker.FontAttributesProperty, new Binding(nameof(FontAttributes), source: this));
        _timePickerView.SetBinding(TimePicker.FormatProperty, new Binding(nameof(TimeFormat), source: this));
        _timePickerView.SetBinding(TimePicker.TextColorProperty, new Binding(nameof(TextColor), source: this));
        _timePickerView.SetBinding(TimePicker.HorizontalOptionsProperty, new Binding(nameof(TextHorizontalOptions), source: this));
        _timePickerView.SetBinding(TimePicker.VerticalOptionsProperty, new Binding(nameof(TextVerticalOptions), source: this));

        _datePickerView.DateSelected += OnDatePickerDateSelected;
        _timePickerView.PropertyChanged += OnTimePickerPropertyChanged;
    }

    private void OnDatePickerDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (_isSyncing)
            return;

        _isSyncing = true;
        var newDate = e.NewDate ?? System.DateTime.Today;
        DateTime = new System.DateTime(
            newDate.Year,
            newDate.Month,
            newDate.Day,
            DateTime.Hour,
            DateTime.Minute,
            DateTime.Second
        );
        DateTimeOffset = ToDateTimeOffset(DateTime);
        _isSyncing = false;

        OnDateTimeUpdated();
    }

    private void OnTimePickerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncing || e.PropertyName != nameof(TimePicker.Time))
            return;

        _isSyncing = true;
        var time = _timePickerView.Time;
        var date = DateTime;
        DateTime = new System.DateTime(
            date.Year,
            date.Month,
            date.Day,
            time?.Hours ?? 0,
            time?.Minutes ?? 0,
            time?.Seconds ?? 0
        );
        DateTimeOffset = ToDateTimeOffset(DateTime);
        _isSyncing = false;

        OnDateTimeUpdated();
    }

    protected override object? GetValueForValidator() => DateTime;

    protected virtual void OnDateTimeChanged(DateTime newValue)
    {
        if (_isSyncing)
            return;

        _isSyncing = true;
        DateTimeOffset = ToDateTimeOffset(newValue);
        SyncPickerViews();
        _isSyncing = false;

        OnDateTimeUpdated();
    }

    private void OnDateTimeOffsetChanged(System.DateTimeOffset newValue)
    {
        if (_isSyncing)
            return;

        _isSyncing = true;
        DateTime = newValue.DateTime; // conserve la date/heure telle quelle, sans conversion de fuseau
        DateTimeOffset = ToDateTimeOffset(DateTime);
        SyncPickerViews();
        _isSyncing = false;

        OnDateTimeUpdated();
    }

    internal void OnRangeChanged()
    {
        if (_isSyncing)
            return;

        _isSyncing = true;
        SyncPickerViews();
        _isSyncing = false;

        OnDateTimeUpdated();
    }

    // iOS pousse Min/Max dans l'UIDatePicker natif : si la plage arrive après la date, le natif
    // recale sa valeur sans la remonter à la vue MAUI et l'affichage diverge du binding.
    private void ApplyRangeToPickers()
    {
        var min = MinimumDate.Date;
        var max = MaximumDate.Date;

        // MAUI valide min <= max à chaque affectation : poser d'abord la borne qui élargit la plage.
        if (min > _datePickerView.MaximumDate)
        {
            _datePickerView.MaximumDate = max;
            _datePickerView.MinimumDate = min;
        }
        else
        {
            _datePickerView.MinimumDate = min;
            _datePickerView.MaximumDate = max;
        }
    }

    private void SyncPickerViews()
    {
        ApplyRangeToPickers();

        _datePickerView.Date = DateTime.Date;
        _timePickerView.Time = DateTime.TimeOfDay;
        _datePickerView.Opacity = 1;
        _timePickerView.Opacity = 1;

        // MAUI clampe Date sur la plage : réaligner la valeur exposée sur ce qui est réellement affiché.
        var displayed = (_datePickerView.Date ?? DateTime).Date + DateTime.TimeOfDay;
        if (displayed != DateTime)
            DateTime = displayed;

        DateTimeOffset = ToDateTimeOffset(DateTime);
    }

    private void OnDateTimeUpdated()
    {
        OnPropertyChanged(nameof(DateTime));
        OnPropertyChanged(nameof(DateTimeOffset));
        CheckAndShowValidations();

        _datePickerView.Opacity = 1;
        _timePickerView.Opacity = 1;

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

        if (SecondIcon is not null)
        {
            secondImageIcon.Value.Source = SecondIcon;
            ApplyIconColor(SecondIcon);

            if (!GridContent.Contains(secondImageIcon.Value))
                GridContent.Add(secondImageIcon.Value, column: 2);

            secondImageIcon.Value.IsVisible = SecondIconIsVisible;
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
        DateTime = System.DateTime.Now;
        base.ResetValidation();
    }

    private DateTimeOffset ToDateTimeOffset(DateTime dateTime)
    {
        // Conserver la date/heure telle quelle, sans conversion de fuseau
        return new DateTimeOffset(dateTime, TimeSpan.Zero);
    }
}
