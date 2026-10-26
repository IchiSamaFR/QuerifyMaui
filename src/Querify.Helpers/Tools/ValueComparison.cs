using System.Globalization;

namespace Querify.Helpers.Tools;

/// <summary>
/// Comparaison de valeurs de cellules : nombres et dates compares comme tels, qu'ils arrivent types
/// ou deja formates en texte (culture invariante des resultats SQL, ou culture courante d'une saisie).
/// </summary>
public static class ValueComparison
{
    private const NumberStyles NumberParseStyles = NumberStyles.Float;
    private const DateTimeStyles DateParseStyles = DateTimeStyles.AllowWhiteSpaces;

    /// <summary>
    /// Ramene une valeur a null, <see cref="decimal"/>, <see cref="DateTime"/> ou <see cref="string"/>.
    /// A calculer une fois par valeur avant un tri : le texte n'est analyse qu'ici.
    /// </summary>
    public static object? Normalize(object? value)
    {
        switch (value)
        {
            case null or DBNull:
                return null;
            case decimal number:
                return number;
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            case double or float:
                var real = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return TryToDecimal(real, out var converted) ? converted : real.ToString(CultureInfo.CurrentCulture);
            case DateTime date:
                return date;
            case DateTimeOffset offset:
                return offset.LocalDateTime;
            case DateOnly day:
                return day.ToDateTime(TimeOnly.MinValue);
            case string text:
                return NormalizeText(text);
            default:
                return Convert.ToString(value, CultureInfo.CurrentCulture);
        }
    }

    /// <summary>Ordre de tri de valeurs normalisees : null, puis nombres, dates et textes.</summary>
    public static int Compare(object? x, object? y)
    {
        var kindOrder = GetKind(x).CompareTo(GetKind(y));
        if (kindOrder != 0)
            return kindOrder;

        return (x, y) switch
        {
            (decimal a, decimal b) => a.CompareTo(b),
            (DateTime a, DateTime b) => a.CompareTo(b),
            (string a, string b) => string.Compare(a, b, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase),
            _ => 0,
        };
    }

    /// <summary>Compare deux valeurs normalisees de meme nature ; faux si l'une est null ou si leurs natures different.</summary>
    public static bool TryCompare(object? x, object? y, out int result)
    {
        result = 0;
        if (x is null || y is null || GetKind(x) != GetKind(y))
            return false;

        result = Compare(x, y);
        return true;
    }

    private static object NormalizeText(string text)
    {
        // Nombres : invariant d'abord (cellules SQL), puis culture courante (saisie "1,5").
        if (decimal.TryParse(text, NumberParseStyles, CultureInfo.InvariantCulture, out var number)
            || decimal.TryParse(text, NumberParseStyles, CultureInfo.CurrentCulture, out number))
            return number;

        // Dates : culture courante d'abord ("12/10/2024" saisi en francais), l'ISO des cellules passe partout.
        if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateParseStyles, out var date)
            || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateParseStyles, out date))
            return date;

        return text;
    }

    private static bool TryToDecimal(double value, out decimal result)
    {
        result = 0;
        if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > (double)decimal.MaxValue)
            return false;

        result = (decimal)value;
        return true;
    }

    private static int GetKind(object? value) => value switch
    {
        null => 0,
        decimal => 1,
        DateTime => 2,
        _ => 3,
    };
}
