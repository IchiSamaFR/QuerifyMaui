namespace SqlToolManagerMaui.Helpers.Tools;

/// <param name="BackupPath">Copie du fichier corrompu, ou null si elle a echoue (ou n'avait pas lieu d'etre).</param>
public sealed record JsonFileResult<T>(JsonFileStatus Status, T? Value, Exception? Error = null, string? BackupPath = null);
