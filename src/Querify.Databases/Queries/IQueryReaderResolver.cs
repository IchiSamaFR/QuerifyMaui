using Querify.Databases.Enums;

namespace Querify.Databases.Queries;

/// <summary>Resout le lecteur de requetes d'un moteur.</summary>
public interface IQueryReaderResolver
{
    IQueryReader Resolve(DatabaseProvider provider);
}
