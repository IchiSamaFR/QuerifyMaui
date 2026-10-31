using Querify.Core.Models.Queries;
using Quill.Code.Execution;
using Quill.Code.Scripting;
using System.Diagnostics;

namespace Querify.Languages.Queries;

/// <summary>
/// Bilan d'une execution construit a partir de ce que l'executeur de Quill (SQL, LINQ) remet au fil de l'eau : messages
/// (affiches aussi en direct), erreurs (niveau Error) et jeux de resultats. Un seul modele pour tous les langages.
/// </summary>
public static class QueryExecutionCollector
{
    /// <param name="run">Execution de Quill, sans retour : tout passe par les options recues ; l'annulation est levee.</param>
    /// <param name="liveMessages">Recoit chaque message des son emission, depuis n'importe quel thread.</param>
    public static async Task<QueryExecutionResult> CollectAsync(Func<DataRunOptions, Task> run, IProgress<LogEntry>? liveMessages = null)
    {
        ArgumentNullException.ThrowIfNull(run);

        List<QueryResultSet> resultSets = [];
        List<LogEntry> messages = [];
        var gate = new Lock();
        var hasErrors = false;
        var wasCancelled = false;
        var stopwatch = Stopwatch.StartNew();

        var options = new DataRunOptions
        {
            Log = message =>
            {
                lock (gate)
                {
                    hasErrors |= message.IsError;
                    messages.Add(message);
                }

                liveMessages?.Report(message);
            },
            Data = resultSet =>
            {
                lock (gate)
                    resultSets.Add(resultSet);
            },
        };

        try
        {
            await run(options);
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }

        stopwatch.Stop();

        lock (gate)
        {
            if (wasCancelled)
                messages.Add(new LogEntry(LogLevel.Error, "Requête annulée par l'utilisateur."));

            messages.Add(new LogEntry(LogLevel.Information, $"Heure de fin : {DateTime.Now:yyyy-MM-dd HH:mm:ss} (durée {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff})"));

            // Copies : un script abandonne peut encore ecrire en arriere-plan.
            return new QueryExecutionResult([.. resultSets], [.. messages], stopwatch.Elapsed, hasErrors, wasCancelled);
        }
    }
}
