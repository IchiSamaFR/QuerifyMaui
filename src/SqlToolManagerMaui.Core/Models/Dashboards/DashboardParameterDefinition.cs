using SqlToolManagerMaui.Core.Enums.Dashboards;
using System.Globalization;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>Parametre d'entree du dashboard, utilise dans les requetes sous la forme @Nom.</summary>
public sealed class DashboardParameterDefinition
{
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "yyyyMMdd", "yyyy-MM-dd'T'HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm"];

    public required string Name { get; set; }

    /// <summary>Libelle du champ ; a defaut, le nom.</summary>
    public string? Label { get; set; }

    public DashboardParameterType Type { get; set; } = DashboardParameterType.Text;

    /// <summary>Valeur a l'ouverture (texte saisi) ; vide = NULL.</summary>
    public string? Default { get; set; }

    /// <summary>
    /// Convertit une saisie en valeur typee (null = NULL SQL). Faux, avec la raison dans
    /// <paramref name="error"/>, si la saisie ne correspond pas au type.
    /// </summary>
    public bool TryConvert(string? text, out object? value, out string? error)
    {
        value = null;
        error = null;
        var input = text?.Trim() ?? string.Empty;

        if (input.Length == 0 || string.Equals(input, "NULL", StringComparison.OrdinalIgnoreCase))
            return true;

        switch (Type)
        {
            case DashboardParameterType.Integer when long.TryParse(input, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer):
                value = integer;
                return true;
            case DashboardParameterType.Decimal when decimal.TryParse(input.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var number):
                value = number;
                return true;
            case DashboardParameterType.Date or DashboardParameterType.DateTime when TryParseDate(input, out var date):
                value = Type == DashboardParameterType.Date ? date.Date : date;
                return true;
            case DashboardParameterType.Boolean when TryParseBoolean(input, out var boolean):
                value = boolean;
                return true;
            case DashboardParameterType.Guid when Guid.TryParse(input, out var guid):
                value = guid;
                return true;
            case DashboardParameterType.Text:
                value = text;
                return true;
        }

        error = Type switch
        {
            DashboardParameterType.Integer => "Entier attendu.",
            DashboardParameterType.Decimal => "Nombre attendu.",
            DashboardParameterType.Date => "Date attendue (aaaa-mm-jj).",
            DashboardParameterType.DateTime => "Date et heure attendues (aaaa-mm-jj hh:mm).",
            DashboardParameterType.Boolean => "0 ou 1 attendu.",
            DashboardParameterType.Guid => "GUID attendu.",
            _ => "Valeur invalide.",
        };
        return false;
    }

    private static bool TryParseDate(string input, out DateTime date)
        => DateTime.TryParseExact(input, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
            || DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);

    private static bool TryParseBoolean(string input, out bool value)
    {
        switch (input.ToUpperInvariant())
        {
            case "1" or "TRUE" or "VRAI" or "OUI":
                value = true;
                return true;
            case "0" or "FALSE" or "FAUX" or "NON":
                value = false;
                return true;
            default:
                value = false;
                return false;
        }
    }
}
