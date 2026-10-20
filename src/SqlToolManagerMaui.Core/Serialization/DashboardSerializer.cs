using SqlToolManagerMaui.Core.Models.Dashboards;
using System.Globalization;
using System.Text.Json;

namespace SqlToolManagerMaui.Core.Serialization;

/// <summary>Lecture / ecriture du JSON d'un fichier .schx.</summary>
public static class DashboardSerializer
{
    public const string FILE_EXTENSION = ".schx";

    public static bool IsDashboardFile(string? filePath)
        => string.Equals(Path.GetExtension(filePath), FILE_EXTENSION, StringComparison.OrdinalIgnoreCase);

    /// <summary>JSON indente, fins de ligne LF (celles de l'editeur).</summary>
    public static string Serialize(DashboardDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return JsonSerializer.Serialize(definition, DashboardJsonContext.Default.DashboardDefinition).ReplaceLineEndings("\n");
    }

    /// <summary>Leve <see cref="FormatException"/> avec un message affichable si le fichier est invalide.</summary>
    public static DashboardDefinition Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
            throw new FormatException("Le fichier est vide.");

        DashboardDefinition? definition;
        try
        {
            definition = JsonSerializer.Deserialize(json, DashboardJsonContext.Default.DashboardDefinition);
        }
        catch (JsonException ex)
        {
            var line = ex.LineNumber is { } number ? $" (ligne {(number + 1).ToString(CultureInfo.InvariantCulture)})" : string.Empty;
            throw new FormatException($"Fichier .schx invalide{line} : {ex.Message}", ex);
        }

        if (definition is null)
            throw new FormatException("Le fichier est vide.");

        if (definition.Version > DashboardDefinition.CURRENT_VERSION)
            throw new FormatException($"Version {definition.Version.ToString(CultureInfo.InvariantCulture)} non prise en charge.");

        if (definition.Pages.Count == 0)
            throw new FormatException("Le dashboard ne contient aucune page.");

        var duplicate = definition.Pages.GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new FormatException($"Page « {duplicate.Key} » déclarée deux fois.");

        return definition;
    }
}
