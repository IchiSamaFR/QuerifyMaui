using CommunityToolkit.Mvvm.Messaging;

namespace Querify.UI.Extensions;

/// <summary>Lancement sans attente d'une tache : son echec est journalise (<c>LogError</c> du messenger) au lieu d'etre perdu.</summary>
public static class FireAndForgetExtensions
{
    /// <param name="errorMessage">Message affiche en cas d'echec ; a defaut, celui de l'exception.</param>
    public static void FireAndForget(this Task task, string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.IsCompleted)
        {
            Observe(task, errorMessage);
            return;
        }

        task.ContinueWith(t => Observe(t, errorMessage), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private static void Observe(Task task, string? errorMessage)
    {
        // Annulee : sortie normale (onglet ferme, demande remplacee), rien a signaler.
        if (!task.IsFaulted || task.Exception is not { } aggregate)
            return;

        Exception exception = aggregate.InnerExceptions.Count == 1 ? aggregate.InnerExceptions[0] : aggregate;
        WeakReferenceMessenger.Default.LogError(exception, errorMessage ?? exception.Message);
    }
}
