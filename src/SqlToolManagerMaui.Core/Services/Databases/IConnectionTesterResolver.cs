using SqlToolManagerMaui.Core.Enums.Databases.Connections;

namespace SqlToolManagerMaui.Core.Services.Databases;

/// <summary>
/// Resout le testeur correspondant a un moteur. Un nouveau moteur se declare
/// uniquement par son enregistrement en tant que <see cref="IConnectionTester"/>.
/// </summary>
public interface IConnectionTesterResolver
{
    IConnectionTester Resolve(DatabaseProvider provider);
}
