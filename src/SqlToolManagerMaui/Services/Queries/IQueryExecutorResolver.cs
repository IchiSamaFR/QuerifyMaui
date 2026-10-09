using SqlToolManagerMaui.Core.Models.Databases.Connections;

namespace SqlToolManagerMaui.Services.Queries;

/// <summary>
/// Resout l'executeur correspondant a un moteur. Un nouveau moteur se declare
/// uniquement par son enregistrement en tant que <see cref="IQueryExecutor"/>.
/// </summary>
public interface IQueryExecutorResolver
{
    IQueryExecutor Resolve(DatabaseProvider provider);
}
