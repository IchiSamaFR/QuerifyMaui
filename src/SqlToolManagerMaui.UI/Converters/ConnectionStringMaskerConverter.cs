using SqlToolManagerMaui.Helpers.Tools;
using System.Globalization;

namespace SqlToolManagerMaui.UI.Converters;

/// <summary>
/// Masque le mot de passe d'une chaine de connexion pour l'affichage, et le restaure
/// quand l'utilisateur n'a pas remplace le masque.
///
/// En binding TwoWay, l'instance retient la derniere valeur source vue par
/// <see cref="Convert"/> pour pouvoir la rendre dans <see cref="ConvertBack"/> : un
/// convertisseur par champ edite (declare dans les ressources de la page, pas en
/// ressource globale partagee).
/// </summary>
public sealed class ConnectionStringMaskerConverter : IValueConverter
{
    /// <summary>Derniere chaine complete recue de la source.</summary>
    private string? _source;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        _source = value as string;
        return ConnectionStringPasswordHelper.Mask(_source);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ConnectionStringPasswordHelper.Unmask(value as string, _source);
}