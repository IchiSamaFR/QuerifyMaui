
using SqlToolManagerMaui.Databases.Models.Queries;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>Ouverture d'une page, en donnant une valeur a certains parametres.</summary>
public sealed class DashboardNavigationDefinition
{
    public required string Page { get; set; }

    public List<DashboardParameterMapping> Parameters { get; init; } = [];

    /// <summary>
    /// Valeurs a donner aux parametres : colonne de la ligne cliquee, autre parametre (@Nom) ou texte fixe.
    /// Une colonne absente (bouton, sans ligne) laisse le parametre inchange.
    /// </summary>
    public Dictionary<string, string?> Resolve(IReadOnlyDictionary<string, string?> current, QueryResultSet? resultSet, QueryResultRow? row)
    {
        ArgumentNullException.ThrowIfNull(current);

        Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in Parameters)
        {
            if (mapping.Value is { } value)
            {
                values[mapping.Parameter] = value.StartsWith('@') && current.TryGetValue(value[1..], out var parameter)
                    ? parameter
                    : value;
            }
            else if (mapping.Column is { } column && resultSet is not null && row is not null)
            {
                var index = resultSet.Columns.ToList().FindIndex(c => string.Equals(c.Name, column, StringComparison.OrdinalIgnoreCase));
                if (index >= 0 && index < row.Cells.Length)
                    values[mapping.Parameter] = row.Cells[index];
            }
        }
        return values;
    }

    /// <summary>Texte du concepteur : une affectation par ligne, « Parametre = Colonne », « = @Param » ou « = 'texte' ».</summary>
    public string FormatParameters()
        => string.Join('\n', Parameters.Select(m => m.Value is { } value
            ? $"{m.Parameter} = {(value.StartsWith('@') ? value : $"'{value}'")}"
            : $"{m.Parameter} = {m.Column}"));

    public static List<DashboardParameterMapping> ParseParameters(string? text)
    {
        List<DashboardParameterMapping> mappings = [];
        foreach (var line in (text ?? string.Empty).Split(['\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
                continue;

            var name = line[..separator].Trim().TrimStart('@');
            var source = line[(separator + 1)..].Trim();
            if (name.Length == 0 || source.Length == 0)
                continue;

            mappings.Add(source switch
            {
                ['\'', .., '\''] when source.Length >= 2 => new DashboardParameterMapping { Parameter = name, Value = source[1..^1] },
                ['@', ..] => new DashboardParameterMapping { Parameter = name, Value = source },
                _ => new DashboardParameterMapping { Parameter = name, Column = source },
            });
        }
        return mappings;
    }
}
