namespace SqlToolManagerMaui.Helpers.Tools;

/// <summary>
/// Masque / restaure le mot de passe d'une chaine de connexion.
///
/// Le reste de la chaine est laisse caractere pour caractere : seule la valeur du mot de
/// passe est substituee. Les mots-cles Password / Pwd sont communs a SQL Server,
/// PostgreSQL et MySQL.
/// </summary>
public static class ConnectionStringPasswordHelper
{
    /// <summary>Valeur affichee a la place du mot de passe.</summary>
    public const string MaskValue = "********";

    #region Public API
    /// <summary>Vrai si la chaine contient un mot de passe (masque ou non).</summary>
    public static bool HasPassword(string? connectionString)
        => FindPasswordValue(connectionString) is not null;

    /// <summary>Remplace le mot de passe par <see cref="MaskValue"/>.</summary>
    public static string Mask(string? connectionString)
        => Replace(connectionString, _ => MaskValue);

    /// <summary>
    /// Rend a la chaine editee le mot de passe d'origine tant que le masque n'a pas ete
    /// remplace. Un mot de passe reellement ressaisi est conserve tel quel.
    /// </summary>
    public static string Unmask(string? editedConnectionString, string? originalConnectionString)
    {
        if (string.IsNullOrEmpty(editedConnectionString))
            return editedConnectionString ?? string.Empty;

        var original = ReadPassword(originalConnectionString);
        if (original is null)
            return editedConnectionString;

        return Replace(editedConnectionString, current => current == MaskValue ? original : current);
    }
    #endregion

    #region Implémentation
    private static string? ReadPassword(string? connectionString)
    {
        var span = FindPasswordValue(connectionString);
        if (span is null)
            return null;

        var (start, length) = span.Value;
        return Unquote(connectionString!.Substring(start, length).Trim());
    }

    private static string Replace(string? connectionString, Func<string, string> replacement)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString ?? string.Empty;

        var span = FindPasswordValue(connectionString);
        if (span is null)
            return connectionString;

        var (start, length) = span.Value;
        var current = Unquote(connectionString.Substring(start, length).Trim());
        var updated = replacement(current);

        if (updated == current)
            return connectionString;

        return string.Concat(
            connectionString.AsSpan(0, start),
            Quote(updated),
            connectionString.AsSpan(start + length));
    }

    /// <summary>Position et longueur de la valeur (brute, quotes comprises) du mot de passe.</summary>
    private static (int Start, int Length)? FindPasswordValue(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return null;

        var segmentStart = 0;
        var quote = '\0';

        for (var i = 0; i <= connectionString.Length; i++)
        {
            if (i < connectionString.Length)
            {
                var c = connectionString[i];

                if (quote != '\0')
                {
                    if (c == quote)
                        quote = '\0';
                    continue;
                }

                if (c is '\'' or '"')
                {
                    quote = c;
                    continue;
                }

                if (c != ';')
                    continue;
            }

            var found = ReadSegment(connectionString, segmentStart, i);
            if (found is not null)
                return found;

            segmentStart = i + 1;
        }

        return null;
    }

    private static (int Start, int Length)? ReadSegment(string connectionString, int start, int end)
    {
        var separator = connectionString.IndexOf('=', start);
        if (separator < 0 || separator >= end)
            return null;

        var key = connectionString[start..separator].Trim();
        if (!key.Equals("password", StringComparison.OrdinalIgnoreCase)
            && !key.Equals("pwd", StringComparison.OrdinalIgnoreCase))
            return null;

        return (separator + 1, end - separator - 1);
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && (value[0] is '\'' or '"') && value[^1] == value[0])
            return value[1..^1];

        return value;
    }

    /// <summary>Les caracteres de structure imposent des quotes, sinon la chaine devient illisible.</summary>
    private static string Quote(string value)
    {
        if (value.Length == 0)
            return value;

        var needsQuotes = value.Contains(';') || value.Contains('=')
            || value[0] is '\'' or '"' or ' ' || value[^1] == ' ';

        if (!needsQuotes)
            return value;

        if (!value.Contains('"'))
            return $"\"{value}\"";

        // Regle ADO.NET : les quotes doubles internes se doublent.
        return value.Contains('\'')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : $"'{value}'";
    }
    #endregion
}