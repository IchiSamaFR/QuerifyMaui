using System.Globalization;

namespace Querify.Helpers.Tools;

/// <summary>
/// Filtre saisi sur une colonne :
/// texte = contient, !texte = ne contient pas, =, !=, &lt;&gt;, &gt;, &gt;=, &lt;, &lt;= = comparaison
/// (numerique ou date quand les deux cotes le sont), NULL / !NULL = valeur absente / presente.
/// </summary>
public sealed class TextFilter
{
    private const string NullKeyword = "NULL";
    private const string NotNullKeyword = "!NULL";
    private const CompareOptions ContainsOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    // Prefixes les plus longs d'abord : ">=" avant ">".
    private static readonly (string Prefix, TextFilterOperator Operator)[] Prefixes =
    [
        (">=", TextFilterOperator.GreaterThanOrEqual),
        ("<=", TextFilterOperator.LessThanOrEqual),
        ("!=", TextFilterOperator.NotEqual),
        ("<>", TextFilterOperator.NotEqual),
        (">", TextFilterOperator.GreaterThan),
        ("<", TextFilterOperator.LessThan),
        ("=", TextFilterOperator.Equal),
        ("!", TextFilterOperator.NotContains),
    ];

    public static TextFilter Empty { get; } = new(TextFilterOperator.None, string.Empty);

    public TextFilterOperator Operator { get; }

    /// <summary>Texte saisi apres l'operateur.</summary>
    public string Operand { get; }

    public bool IsEmpty => Operator == TextFilterOperator.None;

    private readonly object? _normalizedOperand;

    private TextFilter(TextFilterOperator filterOperator, string operand)
    {
        Operator = filterOperator;
        Operand = operand;
        _normalizedOperand = ValueComparison.Normalize(operand);
    }

    public static TextFilter Parse(string? text)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Empty;

        if (trimmed.Equals(NullKeyword, StringComparison.OrdinalIgnoreCase))
            return new TextFilter(TextFilterOperator.IsNull, string.Empty);

        if (trimmed.Equals(NotNullKeyword, StringComparison.OrdinalIgnoreCase))
            return new TextFilter(TextFilterOperator.IsNotNull, string.Empty);

        foreach (var (prefix, filterOperator) in Prefixes)
        {
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            // Un operateur seul (saisie en cours) ne filtre rien.
            var operand = trimmed[prefix.Length..].Trim();
            return operand.Length == 0 ? Empty : new TextFilter(filterOperator, operand);
        }

        return new TextFilter(TextFilterOperator.Contains, trimmed);
    }

    /// <param name="value">Valeur brute de la cellule (null = NULL).</param>
    /// <param name="displayText">Texte affiche, utilise par Contient / Ne contient pas.</param>
    public bool Matches(object? value, string? displayText)
    {
        if (value is null or DBNull)
        {
            return Operator is TextFilterOperator.None or TextFilterOperator.IsNull
                or TextFilterOperator.NotContains or TextFilterOperator.NotEqual;
        }

        return Operator switch
        {
            TextFilterOperator.None or TextFilterOperator.IsNotNull => true,
            TextFilterOperator.IsNull => false,
            TextFilterOperator.Contains => Contains(displayText),
            TextFilterOperator.NotContains => !Contains(displayText),
            TextFilterOperator.Equal => CompareTo(value) == 0,
            TextFilterOperator.NotEqual => CompareTo(value) != 0,
            TextFilterOperator.GreaterThan => CompareTo(value) > 0,
            TextFilterOperator.GreaterThanOrEqual => CompareTo(value) >= 0,
            TextFilterOperator.LessThan => CompareTo(value) < 0,
            TextFilterOperator.LessThanOrEqual => CompareTo(value) <= 0,
            _ => true,
        };
    }

    private bool Contains(string? displayText)
        => displayText is not null
            && CultureInfo.CurrentCulture.CompareInfo.IndexOf(displayText, Operand, ContainsOptions) >= 0;

    /// <summary>Position de la valeur par rapport a l'operande ; null quand leurs natures different (jamais egales ni ordonnees).</summary>
    private int? CompareTo(object value)
        => ValueComparison.TryCompare(ValueComparison.Normalize(value), _normalizedOperand, out var result) ? result : null;
}
