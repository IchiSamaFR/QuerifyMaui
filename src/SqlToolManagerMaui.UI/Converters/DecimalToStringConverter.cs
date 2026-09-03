using System.Globalization;

namespace SqlToolManagerMaui.UI.Converters;

public class DecimalToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal)
        {
            return ((decimal)value).ToString(parameter?.ToString());
        }
        if (value is float)
        {
            return ((float)value).ToString(parameter?.ToString());
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal)
        {
            return ((decimal)value).ToString(parameter?.ToString());
        }
        if (value is float)
        {
            return ((float)value).ToString(parameter?.ToString());
        }
        return value?.ToString();
    }
}
