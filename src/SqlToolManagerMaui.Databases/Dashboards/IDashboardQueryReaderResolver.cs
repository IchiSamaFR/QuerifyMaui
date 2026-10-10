using SqlToolManagerMaui.Core.Enums.Databases.Connections;

namespace SqlToolManagerMaui.Databases.Dashboards;

/// <summary>Resout le lecteur de cartes d'un moteur.</summary>
public interface IDashboardQueryReaderResolver
{
    IDashboardQueryReader Resolve(DatabaseProvider provider);
}
