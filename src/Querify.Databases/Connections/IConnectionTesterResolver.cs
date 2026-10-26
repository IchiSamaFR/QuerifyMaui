
using Querify.Databases.Enums;

namespace Querify.Databases.Connections;

/// <summary>
/// Resout le testeur correspondant a un moteur. Un nouveau moteur se declare
/// uniquement par son enregistrement en tant que <see cref="IConnectionTester"/>.
/// </summary>
public interface IConnectionTesterResolver
{
    IConnectionTester Resolve(DatabaseProvider provider);
}
