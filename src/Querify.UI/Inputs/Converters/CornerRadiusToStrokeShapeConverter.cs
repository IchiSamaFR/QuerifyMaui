using Microsoft.Maui.Controls.Shapes;

namespace Querify.UI.Inputs.Converters;

internal sealed class CornerRadiusToStrokeShapeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => new RoundRectangle { CornerRadius = new CornerRadius(value is double d ? d : 8d) };

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
