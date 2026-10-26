using System.Globalization;

namespace Querify.UI.Converters;

public class IsAllTrueConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        foreach (var value in values)
        {
            if (value is bool b)
            {
                if (!b) return false;
            }
            else if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            {
                return false;
            }
        }
        return true;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
