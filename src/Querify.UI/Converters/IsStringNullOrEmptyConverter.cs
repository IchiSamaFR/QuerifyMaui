using System.Globalization;

namespace Querify.UI.Converters;

public class IsStringNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter != null && parameter is string strParam && strParam.ToLower() == "invert")
        {
            return !string.IsNullOrEmpty(value as string);
        }
        return string.IsNullOrEmpty(value as string);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
