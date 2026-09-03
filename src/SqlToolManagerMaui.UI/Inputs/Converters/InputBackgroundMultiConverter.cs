namespace SqlToolManagerMaui.UI.Inputs.Converters;

internal sealed class InputBackgroundMultiConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is Brush brush)
            return brush;
        if (values.Length > 1 && values[1] is Color color)
            return new SolidColorBrush(color);
        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
