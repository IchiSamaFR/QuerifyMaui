namespace Querify.UI.Inputs.Converters;

/// <summary>
/// Inverse un Padding en Margin negative : le Padding d'un Border s'applique a son contenu,
/// donc aussi au voile de survol ; la marge opposee le fait ressortir jusqu'aux bords,
/// ou la StrokeShape le decoupe.
/// </summary>
internal sealed class PaddingToNegativeMarginConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => value is Thickness p
            ? new Thickness(-p.Left, -p.Top, -p.Right, -p.Bottom)
            : Thickness.Zero;

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}
