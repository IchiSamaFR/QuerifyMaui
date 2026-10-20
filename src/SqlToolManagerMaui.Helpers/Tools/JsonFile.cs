using Newtonsoft.Json;

namespace SqlToolManagerMaui.Helpers.Tools;

/// <summary>
/// Fichiers JSON de l'application (connexions, preferences, onglets) : ecriture atomique, et un
/// fichier corrompu est mis de cote avant que l'appelant ne decide de repartir de zero.
/// </summary>
public static class JsonFile
{
    public static string Serialize<T>(T value) => JsonConvert.SerializeObject(value, Formatting.Indented);

    public static async Task<JsonFileResult<T>> ReadAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            return new JsonFileResult<T>(JsonFileStatus.Missing, default);

        string json;
        try
        {
            json = await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new JsonFileResult<T>(JsonFileStatus.Unreadable, default, ex);
        }

        try
        {
            return new JsonFileResult<T>(JsonFileStatus.Loaded, JsonConvert.DeserializeObject<T>(json));
        }
        catch (JsonException ex)
        {
            return new JsonFileResult<T>(JsonFileStatus.Corrupted, default, ex, CorruptFileHelper.Backup(path));
        }
    }

    /// <summary>Ecriture atomique : un arret en cours d'ecriture ne laisse pas un JSON tronque.</summary>
    public static void WriteAllText(string path, string json)
    {
        var tempPath = PrepareTempPath(path);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }

    /// <inheritdoc cref="WriteAllText"/>
    public static async Task WriteAllTextAsync(string path, string json, CancellationToken cancellationToken = default)
    {
        var tempPath = PrepareTempPath(path);
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, path, overwrite: true);
    }

    private static string PrepareTempPath(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        return path + ".tmp";
    }
}
