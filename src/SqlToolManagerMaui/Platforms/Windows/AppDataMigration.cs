namespace SqlToolManagerMaui.WinUI;

/// <summary>
/// L'ApplicationId nomme le dossier de donnees (%LOCALAPPDATA%\&lt;editeur&gt;\&lt;ApplicationId&gt; : Data, Settings, coffre).
/// Apres un changement d'identifiant, l'ancien dossier est recopie au premier lancement ; il reste en place.
/// </summary>
public static class AppDataMigration
{
    /// <summary>Erreur de copie a journaliser une fois le journal pret ; null si rien a faire ou copie reussie.</summary>
    public static Exception? MigrateFrom(string previousApplicationId)
    {
        string? tempRoot = null;
        try
        {
            // AppDataDirectory = ...\<ApplicationId>\Data
            var currentRoot = Path.GetDirectoryName(FileSystem.AppDataDirectory)!;
            var previousRoot = Path.Combine(Path.GetDirectoryName(currentRoot)!, previousApplicationId);

            if (!Directory.Exists(previousRoot) || HasFiles(currentRoot))
                return null;

            // Copie dans un dossier temporaire puis renommage : une copie interrompue sera refaite au lancement suivant.
            tempRoot = currentRoot + ".migration";
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);

            CopyDirectory(previousRoot, tempRoot);

            // Seuls des dossiers vides ont pu y etre crees (AppDataDirectory) : aucune donnee perdue.
            if (Directory.Exists(currentRoot))
                Directory.Delete(currentRoot, recursive: true);

            Directory.Move(tempRoot, currentRoot);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (tempRoot is not null)
                TryDelete(tempRoot);
            return ex;
        }
    }

    private static bool HasFiles(string directory)
        => Directory.Exists(directory) && Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Any();

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));

        foreach (var directory in Directory.EnumerateDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }

    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Dossier temporaire residuel : supprime a la prochaine tentative.
        }
    }
}
