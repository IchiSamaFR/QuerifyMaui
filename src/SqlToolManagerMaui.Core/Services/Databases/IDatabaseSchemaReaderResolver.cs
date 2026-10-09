using SqlToolManagerMaui.Core.Enums.Databases.Connections;

namespace SqlToolManagerMaui.Core.Services.Databases;

/// <summary>Resout le lecteur de catalogue d'un moteur.</summary>
public interface IDatabaseSchemaReaderResolver
{
    IDatabaseSchemaReader Resolve(DatabaseProvider provider);
}
