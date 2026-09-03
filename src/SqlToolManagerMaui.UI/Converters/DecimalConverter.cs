using System.Globalization;

namespace SqlToolManagerMaui.UI.Converters;

public class DecimalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value?.ToString();
        return s is null ? null : s.Replace(',', '.');
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value?.ToString();
        return s is null ? null : s.Replace(',', '.');
    }
}