using Microsoft.Maui.Controls.Shapes;
using SqlToolManagerMaui.UI.Inputs.Enums;

namespace SqlToolManagerMaui.UI.Inputs.Converters;

internal sealed class BoxShapeMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        var shape = values.Length > 0 && values[0] is PinInputBoxShape s ? s : PinInputBoxShape.RoundCorner;
        var size = values.Length > 1 && values[1] is double sz ? sz : 48d;
        // values[2] = BoxCornerRadius (-1 = hérité), values[3] = CornerRadius du champ.
        var boxRadius = values.Length > 2 && values[2] is double br ? br : -1d;
        var fieldRadius = values.Length > 3 && values[3] is double fr ? fr : 8d;
        var radius = boxRadius >= 0 ? boxRadius : fieldRadius;

        return new RoundRectangle
        {
            CornerRadius = shape switch
            {
                PinInputBoxShape.Square => new CornerRadius(0),
                PinInputBoxShape.Circle => new CornerRadius(size / 2),
                _ => new CornerRadius(radius),
            },
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
