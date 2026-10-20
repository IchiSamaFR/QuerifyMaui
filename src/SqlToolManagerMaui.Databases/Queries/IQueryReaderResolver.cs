using SqlToolManagerMaui.Databases.Enums;

namespace SqlToolManagerMaui.Databases.Queries;

/// <summary>Resout le lecteur de requetes d'un moteur.</summary>
public interface IQueryReaderResolver
{
    IQueryReader Resolve(DatabaseProvider provider);
}
