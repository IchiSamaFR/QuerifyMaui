using SqlToolManagerMaui.Core.Models.Editors;

namespace SqlToolManagerMaui.Core.Models.Settings;

/// <summary>Preferences de l'utilisateur, conservees d'une session a l'autre.</summary>
public sealed class UserPreferences
{
    public const int MaxRecentFiles = 10;

    /// <summary>Chemins des derniers fichiers ouverts ou enregistres, du plus recent au plus ancien.</summary>
    public List<string> RecentFiles { get; set; } = [];

    /// <summary>Options d'affichage de l'editeur.</summary>
    public EditorConfiguration Editor { get; set; } = EditorConfiguration.CreateDefault();

    /// <summary>Place le fichier en tete de liste (sans doublon) et tronque au-dela de <see cref="MaxRecentFiles"/>.</summary>
    public void AddRecentFile(string filePath)
    {
        RemoveRecentFile(filePath);
        RecentFiles.Insert(0, filePath);

        if (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveRange(MaxRecentFiles, RecentFiles.Count - MaxRecentFiles);
    }

    public bool RemoveRecentFile(string filePath)
        => RecentFiles.RemoveAll(p => string.Equals(p, filePath, StringComparison.OrdinalIgnoreCase)) > 0;

    public UserPreferences Clone() => new()
    {
        RecentFiles = [.. RecentFiles],
        Editor = Editor.Clone(),
    };
}
