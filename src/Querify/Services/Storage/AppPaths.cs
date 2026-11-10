namespace Querify.Services.Storage;

/// <summary>Emplacements des donnees de l'application.</summary>
public static class AppPaths
{
    /// <summary>%LOCALAPPDATA%\querify\Data ; remplace AppDataDirectory, qui insere le nom de l'editeur dans le chemin.</summary>
    public static string DataDirectory { get; } = Directory.CreateDirectory(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "querify", "Data")).FullName;
}
