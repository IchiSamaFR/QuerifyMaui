using Quill.Code.Execution;
using System.Globalization;
using Quill.Code.Scripting;

namespace Querify.Core.Models.Queries;

/// <summary>Bilan complet d'une execution, tel que l'affiche l'application : jeux de resultats, messages et duree.</summary>
public sealed class QueryExecutionResult
{
    public IReadOnlyList<QueryResultSet> ResultSets { get; }

    public IReadOnlyList<LogEntry> Messages { get; }

    public TimeSpan Elapsed { get; }

    /// <summary>Vrai si au moins un lot a echoue (les lots suivants sont tout de meme executes).</summary>
    public bool HasErrors { get; }

    public bool WasCancelled { get; }

    /// <summary>Duree au format SSMS (hh:mm:ss.fff).</summary>
    public string ElapsedLabel => Elapsed.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);

    /// <summary>Libelle de fin d'execution pour la barre d'etat.</summary>
    public string StatusLabel => (WasCancelled, HasErrors) switch
    {
        (true, _) => $"Requête annulée ({ElapsedLabel}).",
        (_, true) => $"Requête terminée avec des erreurs ({ElapsedLabel}).",
        _ => $"Requête exécutée avec succès ({ElapsedLabel}).",
    };

    public QueryExecutionResult(IReadOnlyList<QueryResultSet> resultSets, IReadOnlyList<LogEntry> messages, TimeSpan elapsed, bool hasErrors, bool wasCancelled)
    {
        ResultSets = resultSets;
        Messages = messages;
        Elapsed = elapsed;
        HasErrors = hasErrors;
        WasCancelled = wasCancelled;
    }
}
