namespace Querify.UI.Inputs.Converters;

/// <summary>
/// Rayon uniforme (double) vers <see cref="CornerRadius"/>, pour les controles dont la
/// propriete n'accepte pas un simple double (BoxView.CornerRadius, par exemple).
/// </summary>
internal sealed class DoubleToCornerRadiusConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => new CornerRadius(value is double d ? d : 0d);

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
