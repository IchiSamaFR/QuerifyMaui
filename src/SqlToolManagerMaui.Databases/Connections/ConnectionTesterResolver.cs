using SqlToolManagerMaui.Databases.Enums;

namespace SqlToolManagerMaui.Databases.Connections;

/// <inheritdoc cref="IConnectionTesterResolver"/>
public sealed class ConnectionTesterResolver : IConnectionTesterResolver
{
    private readonly Dictionary<DatabaseProvider, IConnectionTester> _testers;

    public ConnectionTesterResolver(IEnumerable<IConnectionTester> testers)
    {
        ArgumentNullException.ThrowIfNull(testers);
        _testers = testers.ToDictionary(t => t.Provider);
    }

    public IConnectionTester Resolve(DatabaseProvider provider)
        => _testers.TryGetValue(provider, out var tester)
            ? tester
            : throw new NotSupportedException($"Aucun testeur enregistre pour le moteur {provider}.");
}
