using SqlToolManagerMaui.Databases.Common;
using SqlToolManagerMaui.Databases.Enums;

namespace SqlToolManagerMaui.Databases.Connections;

/// <inheritdoc cref="IConnectionTesterResolver"/>
public sealed class ConnectionTesterResolver : IConnectionTesterResolver
{
    private readonly KeyedServiceMap<DatabaseProvider, IConnectionTester> _testers;

    public ConnectionTesterResolver(IEnumerable<IConnectionTester> testers)
    {
        _testers = new(testers, t => t.Provider, "testeur de connexion");
    }

    public IConnectionTester Resolve(DatabaseProvider provider) => _testers.Resolve(provider);
}
