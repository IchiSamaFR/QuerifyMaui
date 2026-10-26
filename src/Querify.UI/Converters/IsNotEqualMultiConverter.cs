using System.Globalization;

namespace Querify.UI.Converters;

public class IsNotEqualMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2)
            return true;

        return !AreEqual(values[0], values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        if (left.Equals(right))
            return true;

        if (IsNumeric(left) && IsNumeric(right))
        {
            try
            {
                return System.Convert.ToDecimal(left) == System.Convert.ToDecimal(right);
            }
            catch { }
        }

        return string.Equals(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNumeric(object obj) =>
        obj is byte or sbyte
            or short or ushort
            or int or uint
            or long or ulong
            or float or double or decimal;
}