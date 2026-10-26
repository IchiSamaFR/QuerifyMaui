using CommunityToolkit.Mvvm.Messaging;
using Querify.Core.Ioc;
using Querify.UI.Extensions;
using Querify.UI.Models.Messagings;
using System.Globalization;
using System.Text;

namespace Querify.Services.Diagnostics;

/// <summary>
/// Ecrit chaque <see cref="WeakLog"/> dans un journal texte quotidien (logs/app-aaaaMMjj.log dans
/// AppDataDirectory) : seule trace d'une erreur une fois la barre d'etat effacee ou l'application fermee.
/// Instancie au demarrage (MauiProgram) pour etre abonne avant la premiere erreur.
/// </summary>
[AutoRegister]
public sealed class FileLogWriter
{
    private const int RetainedDays = 14;

    private readonly string _directory = Path.Combine(FileSystem.AppDataDirectory, "logs");
    private readonly Lock _gate = new();

    public FileLogWriter()
    {
        PurgeOldFiles();
        WeakReferenceMessenger.Default.RegisterLogHandler(this, Write);
    }

    private void Write(WeakLog log)
    {
        var now = DateTime.Now;
        var line = new StringBuilder()
            .Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Append(" [").Append(log.Level).Append("] ")
            .AppendLine(log.Message);

        if (log.Exception is not null)
            line.AppendLine(log.Exception.ToString());

        var path = Path.Combine(_directory, $"app-{now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log");
        try
        {
            lock (_gate)
            {
                Directory.CreateDirectory(_directory);
                File.AppendAllText(path, line.ToString());
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Le journal ne doit jamais faire echouer l'appelant.
        }
    }

    private void PurgeOldFiles()
    {
        try
        {
            if (!Directory.Exists(_directory))
                return;

            var limit = DateTime.Now.AddDays(-RetainedDays);
            foreach (var file in Directory.EnumerateFiles(_directory, "app-*.log"))
            {
                if (File.GetLastWriteTime(file) < limit)
                    File.Delete(file);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Purge au mieux : un ancien journal verrouille sera supprime au prochain demarrage.
        }
    }
}
