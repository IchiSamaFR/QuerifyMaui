using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Languages.Commons.Databases;

namespace SqlToolManagerMaui.Languages.Databases;

/// <summary>Resout le lecteur de catalogue d'un moteur.</summary>
public interface IDatabaseSchemaReaderResolver
{
    IDatabaseSchemaReader Resolve(DatabaseProvider provider);
}
