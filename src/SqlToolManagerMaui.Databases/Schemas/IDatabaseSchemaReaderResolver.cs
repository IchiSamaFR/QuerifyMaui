
using SqlToolManagerMaui.Databases.Enums;

namespace SqlToolManagerMaui.Databases.Schemas;

/// <summary>Resout le lecteur de catalogue d'un moteur.</summary>
public interface IDatabaseSchemaReaderResolver
{
    IDatabaseSchemaReader Resolve(DatabaseProvider provider);
}
