using System.Globalization;

namespace SqlToolManagerMaui.UI.Converters;

/// <summary>Vrai si les deux valeurs sont la meme instance (et non deux objets egaux).</summary>
public class IsSameMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        => values is { Length: >= 2 } && values[0] is not null && ReferenceEquals(values[0], values[1]);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
