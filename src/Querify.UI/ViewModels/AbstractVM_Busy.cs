using CommunityToolkit.Mvvm.Messaging;
using Querify.UI.Extensions;
using Querify.UI.Models.Messagings;

namespace Querify.UI.ViewModels;

public abstract partial class AbstractVM
{
    // Running tasks lifetime management
    private int _blockingTaskCount = 0;
    private int _backgroundTaskCount = 0;
    private CancellationTokenSource _lifetimeCts = new();

    /// <summary>
    /// Vrai tant que la page n'est pas au premier plan ou qu'une tâche bloquante est en cours.
    /// Piloté exclusivement par <see cref="RunAsBusyAsync(Func{Task}, bool, bool)"/> : lecture seule.
    /// </summary>
    public bool IsBusy => Volatile.Read(ref _blockingTaskCount) > 0;

    /// <summary>
    /// Vrai tant que la page est au premier plan et qu'aucune tâche bloquante n'est en cours.
    /// </summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Vrai tant qu'une tâche de fond est en cours (refresh, notification). N'inhibe pas l'UI.
    /// </summary>
    public bool IsBackgroundBusy => Volatile.Read(ref _backgroundTaskCount) > 0;

    /// <summary>
    /// Annulé lorsque le ViewModel est déchargé.
    /// </summary>
    protected CancellationToken LifetimeToken => Volatile.Read(ref _lifetimeCts).Token;


    /// <summary>
    /// Annule <see cref="LifetimeToken"/> et réarme un token neuf pour les prochaines tâches.
    /// </summary>
    protected void CancelRunningTasks()
    {
        // Pas de Dispose : des tâches encore en vol détiennent le token.
        Interlocked.Exchange(ref _lifetimeCts, new CancellationTokenSource()).Cancel();
    }

    /// <summary>
    /// Exécute une tâche en marquant le ViewModel occupé. Les exceptions sont publiées
    /// (<see cref="WeakLog"/>) : inutile de rajouter un try/catch au niveau de l'appelant.
    /// </summary>
    /// <param name="blocking">False pour un refresh/notification de fond qui ne doit pas inhiber l'UI.</param>
    /// <param name="skipIfBusy">True pour ignorer l'appel si une tâche du même type est déjà en cours (anti double-tap).</param>
    public Task RunAsBusyAsync(Func<Task> taskFunc, bool blocking = true, bool skipIfBusy = false)
        => RunAsBusyAsync(_ => taskFunc(), blocking, skipIfBusy);

    /// <inheritdoc cref="RunAsBusyAsync(Func{Task}, bool, bool)"/>
    public Task RunAsBusyAsync(Func<CancellationToken, Task> taskFunc, bool blocking = true, bool skipIfBusy = false)
        => RunAsBusyAsync<object?>(async ct => { await taskFunc(ct); return null; }, blocking, skipIfBusy);

    /// <inheritdoc cref="RunAsBusyAsync(Func{Task}, bool, bool)"/>
    /// <returns>Le résultat de la tâche, ou <c>default</c> si elle a été ignorée, annulée ou a échoué.</returns>
    public async Task<T?> RunAsBusyAsync<T>(Func<CancellationToken, Task<T>> taskFunc, bool blocking = true, bool skipIfBusy = false)
    {
        // Volontairement basé sur le compteur et non sur IsBusy : un appel déclenché pendant
        // une transition de page (IsFocused == false) ne doit pas être silencieusement ignoré.
        int runningCount = blocking
            ? Volatile.Read(ref _blockingTaskCount)
            : Volatile.Read(ref _backgroundTaskCount);

        if (skipIfBusy && runningCount > 0)
        {
            return default;
        }

        EnterBusy(blocking);
        try
        {
            return await taskFunc(LifetimeToken);
        }
        catch (OperationCanceledException)
        {
            // Sortie de page : ce n'est pas une erreur à remonter à l'utilisateur.
            return default;
        }
        catch (Exception ex)
        {
            // N'interrompt pas l'appelant, mais l'erreur est affichée et journalisée par l'application.
            WeakReferenceMessenger.Default.LogError(ex, ex.Message);
            return default;
        }
        finally
        {
            ExitBusy(blocking);
        }
    }

    /// <summary>
    /// Variante fire-and-forget de <see cref="RunAsBusyAsync(Func{Task}, bool, bool)"/>,
    /// sûre car aucune exception ne peut s'échapper de la tâche.
    /// </summary>
    public void StartAsBusy(Func<Task> taskFunc, bool blocking = true, bool skipIfBusy = false)
        => RunAsBusyAsync(taskFunc, blocking, skipIfBusy).FireAndForget();

    /// <inheritdoc cref="StartAsBusy(Func{Task}, bool, bool)"/>
    public void StartAsBusy(Func<CancellationToken, Task> taskFunc, bool blocking = true, bool skipIfBusy = false)
        => RunAsBusyAsync(taskFunc, blocking, skipIfBusy).FireAndForget();

    private void EnterBusy(bool blocking)
    {
        if (blocking)
        {
            Interlocked.Increment(ref _blockingTaskCount);
        }
        else
        {
            Interlocked.Increment(ref _backgroundTaskCount);
        }
        NotifyBusyChanged(blocking);
    }

    private void ExitBusy(bool blocking)
    {
        if (blocking)
        {
            Interlocked.Decrement(ref _blockingTaskCount);
        }
        else
        {
            Interlocked.Decrement(ref _backgroundTaskCount);
        }
        NotifyBusyChanged(blocking);
    }

    private void NotifyBusyChanged(bool blocking)
    {
        // La continuation peut reprendre sur un thread du pool : le binding doit être notifié sur le thread UI.
        if (MainThread.IsMainThread)
        {
            RaiseBusyChanged(blocking);
            return;
        }
        MainThread.BeginInvokeOnMainThread(() => RaiseBusyChanged(blocking));
    }

    private void RaiseBusyChanged(bool blocking)
    {
        if (!blocking)
        {
            OnPropertyChanged(nameof(IsBackgroundBusy));
            return;
        }

        // IsNotBusy aussi : AbstractPage et AbstractContentView y lient IsEnabled.
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
    }
}
