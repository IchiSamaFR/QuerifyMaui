using Quill.Code.Scripting;

namespace Querify.Services.Editors;

/// <summary>
/// Recoit les messages d'une execution depuis n'importe quel thread et les publie sur le thread UI par lots :
/// une boucle de Log ne doit pas reconstruire l'affichage a chaque message.
/// </summary>
public sealed class LiveQueryMessages : IProgress<LogEntry>
{
    private static readonly TimeSpan PublishInterval = TimeSpan.FromMilliseconds(100);

    private readonly IDispatcher _dispatcher;
    private readonly Action<IReadOnlyList<LogEntry>> _publish;
    private readonly Lock _lock = new();
    private readonly List<LogEntry> _messages = [];
    private bool _isPublishScheduled;
    private bool _isClosed;

    /// <param name="dispatcher">Dispatcher du thread UI.</param>
    /// <param name="publish">Recoit tous les messages recus jusque-la, sur le thread UI.</param>
    public LiveQueryMessages(IDispatcher dispatcher, Action<IReadOnlyList<LogEntry>> publish)
    {
        _dispatcher = dispatcher;
        _publish = publish;
    }

    public void Report(LogEntry value)
    {
        lock (_lock)
        {
            // Un script abandonne peut encore ecrire apres la fin de l'execution.
            if (_isClosed)
                return;

            _messages.Add(value);
            if (_isPublishScheduled)
                return;

            _isPublishScheduled = true;
        }

        _dispatcher.DispatchDelayed(PublishInterval, Publish);
    }

    /// <summary>Arrete les publications : le resultat final contient deja tous les messages.</summary>
    public void Close()
    {
        lock (_lock)
            _isClosed = true;
    }

    private void Publish()
    {
        IReadOnlyList<LogEntry> snapshot;
        lock (_lock)
        {
            _isPublishScheduled = false;
            if (_isClosed)
                return;

            snapshot = [.. _messages];
        }

        _publish(snapshot);
    }
}
