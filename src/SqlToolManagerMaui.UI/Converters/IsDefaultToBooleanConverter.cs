using System.Globalization;

namespace SqlToolManagerMaui.UI.Converters;

public class IsDefaultToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isDefault = IsDefaultValue(value);

        if (parameter is string strParam && strParam.Equals("invert", StringComparison.OrdinalIgnoreCase))
            return !isDefault;

        return isDefault;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException($"{nameof(IsDefaultToBooleanConverter)} does not support ConvertBack.");

    private static bool IsDefaultValue(object? value)
    {
        if (value is null)
            return true;

        Type type = value.GetType();

        if (!type.IsValueType)
            return false;

        return value.Equals(Activator.CreateInstance(type));
    }
}