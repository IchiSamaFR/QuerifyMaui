using SqlToolManagerMaui.Models;
using SqlToolManagerMaui.Models.Databases;

namespace SqlToolManagerMaui.Services.Databases;

/// <summary>
/// Resout le testeur correspondant a un moteur. Un nouveau moteur se declare
/// uniquement par son enregistrement en tant que <see cref="IConnectionTester"/>.
/// </summary>
public interface IConnectionTesterResolver
{
    IConnectionTester Resolve(DatabaseProvider provider);
}

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
