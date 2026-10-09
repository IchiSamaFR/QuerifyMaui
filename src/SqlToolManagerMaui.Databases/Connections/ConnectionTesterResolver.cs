using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Ioc;

namespace SqlToolManagerMaui.Databases.Connections;

/// <inheritdoc cref="IConnectionTesterResolver"/>
[AutoRegister(As = typeof(IConnectionTesterResolver))]
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
