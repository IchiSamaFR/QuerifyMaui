using Querify.Databases.Enums;
using Quill.Data.Sql;

namespace Querify.Databases.Schemas;

/// <summary>Resout le lecteur de catalogue (Quill.Data.Sql) d'un moteur.</summary>
public interface IDatabaseSchemaReaderResolver
{
    ISqlSchemaReader Resolve(DatabaseProvider provider);
}
