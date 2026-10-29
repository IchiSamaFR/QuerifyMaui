using Querify.Databases.Enums;
using Quill.Data.Sql;

namespace Querify.Databases.Schemas;

/// <summary>Lecteur de catalogue de Quill associe au moteur qu'il sait lire (Quill ignore <see cref="DatabaseProvider"/>).</summary>
public sealed record DatabaseSchemaReaderRegistration(DatabaseProvider Provider, ISqlSchemaReader Reader);
