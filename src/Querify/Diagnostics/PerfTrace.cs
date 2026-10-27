using System.Diagnostics;

namespace Querify.Diagnostics;

/// <summary>
/// Mesures temporaires (DEBUG) : chaque etape ecrit sa duree dans la fenetre Sortie de Visual Studio,
/// prefixee de [Perf]. Le chien de garde signale les blocages du thread UI et l'etape en cours a ce moment.
/// A filtrer avec « [Perf] » dans la fenetre Sortie.
/// </summary>
public static class PerfTrace
{
    /// <summary>Au-dela, un tour du thread UI est signale comme blocage.</summary>
    private const int FreezeThresholdMs = 100;

    private static readonly Stopwatch Clock = Stopwatch.StartNew();
    private static IDispatcherTimer? _watchdog;
    private static long _lastTickMs;

    /// <summary>Derniere etape commencee (ou terminee) : ce que le thread UI faisait probablement pendant un blocage.</summary>
    private static volatile string _lastStep = "(aucune)";

    /// <summary>Mesure un bloc : <c>using var _ = PerfTrace.Measure("etape");</c></summary>
    public static Scope Measure(string step) => new(step);

    /// <summary>Repere ponctuel horodate.</summary>
    [Conditional("DEBUG")]
    public static void Mark(string message)
    {
        _lastStep = message;
        Write($"• {message}");
    }

    /// <summary>Lance la mesure de l'appel jusqu'au premier await (partie synchrone, bloque l'appelant) puis jusqu'a la fin.</summary>
    public static async Task<T> MeasureAsync<T>(string step, Func<Task<T>> action)
    {
        var start = Clock.ElapsedMilliseconds;
        _lastStep = step;
        var task = action();
        Write($"  {step} : partie synchrone {Clock.ElapsedMilliseconds - start} ms{ThreadInfo()}");
        try
        {
            return await task;
        }
        finally
        {
            Write($"  {step} : total {Clock.ElapsedMilliseconds - start} ms{ThreadInfo()}");
        }
    }

    /// <inheritdoc cref="MeasureAsync{T}(string, Func{Task{T}})"/>
    public static async Task MeasureAsync(string step, Func<Task> action)
        => await MeasureAsync(step, async () => { await action(); return true; });

    /// <summary>Signale chaque tour du thread UI qui depasse <see cref="FreezeThresholdMs"/>.</summary>
    [Conditional("DEBUG")]
    public static void StartWatchdog(IDispatcher dispatcher)
    {
        if (_watchdog is not null)
            return;

        _lastTickMs = Clock.ElapsedMilliseconds;
        _watchdog = dispatcher.CreateTimer();
        _watchdog.Interval = TimeSpan.FromMilliseconds(16);
        _watchdog.Tick += (_, _) =>
        {
            var now = Clock.ElapsedMilliseconds;
            var gap = now - _lastTickMs;
            _lastTickMs = now;
            if (gap > FreezeThresholdMs)
                Write($"!! UI bloquee ~{gap} ms (derniere etape : {_lastStep})");
        };
        _watchdog.Start();
    }

    private static string ThreadInfo()
        => MainThread.IsMainThread ? " [UI]" : $" [thread {Environment.CurrentManagedThreadId}]";

    private static void Write(string message)
    {
#if DEBUG
        Debug.WriteLine($"[Perf] {Clock.ElapsedMilliseconds,7} ms | {message}");
#endif
    }

    public readonly struct Scope : IDisposable
    {
        private readonly string _step;
        private readonly long _start;

        internal Scope(string step)
        {
            _step = step;
            _start = Clock.ElapsedMilliseconds;
            _lastStep = step;
        }

        public void Dispose()
        {
            var elapsed = Clock.ElapsedMilliseconds - _start;
            Write($"  {_step} : {elapsed} ms{ThreadInfo()}{(elapsed >= FreezeThresholdMs ? "  <-- LENT" : string.Empty)}");
        }
    }
}
