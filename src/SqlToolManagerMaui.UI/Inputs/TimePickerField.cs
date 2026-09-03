using SqlToolManagerMaui.UI.Inputs.Core;
using System.ComponentModel;

namespace SqlToolManagerMaui.UI.Inputs;

/// <summary>
/// Champ TimePicker avec titre flottant, icône et validations.
/// Réimplémentation standalone (sans UraniumUI / Plainer).
/// </summary>
[ContentProperty(nameof(Validations))]
public partial class TimePickerField : InputField
{
    private bool _isSyncing;

    private readonly BorderlessTimePicker _timePickerView = new BorderlessTimePicker
    {
        Margin = new Thickness(0),
        Opacity = 1,
    };

    public BorderlessTimePicker TimePickerView => _timePickerView;

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

    public TimePickerField()
    {
        GridContent.Add(_timePickerView, column: 1);

        Content = GridContent;
        RegisterForEvents();

        OnIconChanged();
        BindPickerViews();
    }

    private void BindPickerViews()
    {
        _timePickerView.SetBinding(TimePicker.IsEnabledProperty, new Binding(nameof(IsEnabled), source: this));
        _timePickerView.SetBinding(TimePicker.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        _timePickerView.SetBinding(TimePicker.FontAutoScalingEnabledProperty, new Binding(nameof(FontAutoScalingEnabled), source: this));
        _timePickerView.SetBinding(TimePicker.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
        _timePickerView.SetBinding(TimePicker.FontAttributesProperty, new Binding(nameof(FontAttributes), source: this));
        _timePickerView.SetBinding(TimePicker.FormatProperty, new Binding(nameof(TimeFormat), source: this));
        _timePickerView.SetBinding(TimePicker.TextColorProperty, new Binding(nameof(TextColor), source: this));
        _timePickerView.SetBinding(TimePicker.HorizontalOptionsProperty, new Binding(nameof(TextHorizontalOptions), source: this));
        _timePickerView.SetBinding(TimePicker.VerticalOptionsProperty, new Binding(nameof(TextVerticalOptions), source: this));

        _timePickerView.PropertyChanged += OnTimePickerPropertyChanged;
    }

    private void OnTimePickerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncing || e.PropertyName != nameof(TimePicker.Time))
            return;

        _isSyncing = true;
        Time = _timePickerView.Time;
        _isSyncing = false;

        OnTimeUpdated();
    }

    protected override object? GetValueForValidator() => Time;

    protected virtual void OnTimeChanged()
    {
        if (_isSyncing)
            return;

        _isSyncing = true;

        if (Time.HasValue)
        {
            _timePickerView.Time = Time.Value;
            _timePickerView.Opacity = 1;
        }
        else
        {
            _timePickerView.Opacity = 0;
        }

        _isSyncing = false;

        OnTimeUpdated();
    }

    private void OnTimeUpdated()
    {
        OnPropertyChanged(nameof(Time));
        CheckAndShowValidations();

        if (Time.HasValue)
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
        Time = System.DateTime.Now.TimeOfDay;
        base.ResetValidation();
    }
}
