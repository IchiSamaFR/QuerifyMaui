using System.Globalization;

namespace Querify.UI.Converters;

public class IsNullObjectConverter : IValueConverter
{
    private object? _cache;
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        _cache = value;
        if (parameter != null && parameter is string strParam && strParam.ToLower() == "invert")
        {
            return value != null;
        }
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var booleanValue = (bool?)value;
        if (parameter != null && parameter is string strParam && strParam.ToLower() == "invert")
        {
            return booleanValue == true ? _cache : null;
        }
        return booleanValue != true ? _cache : null;
    }
}
