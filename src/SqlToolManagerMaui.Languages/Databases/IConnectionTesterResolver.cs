using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Languages.Commons.Databases;

namespace SqlToolManagerMaui.Languages.Databases;

/// <summary>
/// Resout le testeur correspondant a un moteur. Un nouveau moteur se declare
/// uniquement par son enregistrement en tant que <see cref="IConnectionTester"/>.
/// </summary>
public interface IConnectionTesterResolver
{
    IConnectionTester Resolve(DatabaseProvider provider);
}
