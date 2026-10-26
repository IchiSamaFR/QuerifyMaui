using System.Collections;
using System.Globalization;

namespace Querify.UI.Converters;

public class IsAnyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection collection)
            return collection.Count > 0;

        if (value is IEnumerable enumerable)
        {
            foreach (var _ in enumerable)
                return true;
            return false;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
