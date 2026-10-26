using Microsoft.Extensions.Logging;

namespace Querify.UI.Models.Messagings;

/// <summary>Entree de journal publiee par le messenger : affichee (barre d'etat) et ecrite (fichier) par ses abonnes.</summary>
public class WeakLog
{
    public string? Message { get; }
    public Exception? Exception { get; }
    public LogLevel Level { get; }

    public WeakLog(string? message, Exception? exception, LogLevel level)
    {
        Message = message;
        Exception = exception;
        Level = level;
    }
}
