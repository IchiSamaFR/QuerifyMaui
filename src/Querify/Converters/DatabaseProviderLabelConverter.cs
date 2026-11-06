using Querify.Databases.Common;
using Querify.Databases.Enums;
using System.Globalization;

namespace Querify.Converters;

/// <summary>Libelle d'un <see cref="DatabaseProvider"/> (listes deroulantes du moteur).</summary>
public sealed class DatabaseProviderLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DatabaseProvider provider ? DatabaseProviders.GetLabel(provider) : value?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
