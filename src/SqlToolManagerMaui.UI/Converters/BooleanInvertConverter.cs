using System.Globalization;

namespace SqlToolManagerMaui.UI.Converters;

public class BooleanInvertConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var nValue = value is bool ? (value as bool? ?? false) : false;
        return !nValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
