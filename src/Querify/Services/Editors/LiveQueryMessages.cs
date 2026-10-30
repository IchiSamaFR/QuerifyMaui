using Quill.Code.SqlServer.Execution;

namespace Querify.Services.Editors;

/// <summary>
/// Recoit les messages d'une execution depuis n'importe quel thread et les publie sur le thread UI par lots :
/// une boucle de Log ne doit pas reconstruire l'affichage a chaque message.
/// </summary>
public sealed class LiveQueryMessages : IProgress<QueryMessage>
{
    private static readonly TimeSpan PublishInterval = TimeSpan.FromMilliseconds(100);

    private readonly IDispatcher _dispatcher;
    private readonly Action<IReadOnlyList<QueryMessage>> _publish;
    private readonly Lock _lock = new();
    private readonly List<QueryMessage> _messages = [];
    private bool _isPublishScheduled;
    private bool _isClosed;

    /// <param name="dispatcher">Dispatcher du thread UI.</param>
    /// <param name="publish">Recoit tous les messages recus jusque-la, sur le thread UI.</param>
    public LiveQueryMessages(IDispatcher dispatcher, Action<IReadOnlyList<QueryMessage>> publish)
    {
        _dispatcher = dispatcher;
        _publish = publish;
    }

    public void Report(QueryMessage value)
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
        IReadOnlyList<QueryMessage> snapshot;
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
