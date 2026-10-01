using System.Globalization;

namespace SqlToolManagerMaui.UI.Converters;

/// <summary>
/// Niveau d'imbrication (int) vers une marge gauche, pour indenter un arbre rendu a plat.
/// ConverterParameter = largeur d'un niveau en pixels (16 par defaut).
/// </summary>
public class LevelToIndentConverter : IValueConverter
{
    private const double DefaultStep = 16d;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value is int i ? i : 0;
        var step = double.TryParse(parameter?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ? s : DefaultStep;
        return new Thickness(level * step, 0, 0, 0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
