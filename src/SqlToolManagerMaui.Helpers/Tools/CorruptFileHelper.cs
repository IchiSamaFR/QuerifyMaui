using System.Globalization;

namespace SqlToolManagerMaui.Helpers.Tools;

/// <summary>Met de cote un fichier illisible avant qu'une reecriture ne l'efface.</summary>
public static class CorruptFileHelper
{
    /// <summary>Copie le fichier en « nom.corrupt-aaaaMMjj-HHmmss » ; rend le chemin de la copie, ou null si elle a echoue.</summary>
    public static string? Backup(string filePath)
    {
        try
        {
            var backupPath = $"{filePath}.corrupt-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}";
            File.Copy(filePath, backupPath, overwrite: true);
            return backupPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
