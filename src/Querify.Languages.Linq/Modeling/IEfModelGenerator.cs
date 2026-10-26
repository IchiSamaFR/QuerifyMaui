using Querify.Databases.Models.Schemas;

namespace Querify.Languages.Linq.Modeling;

/// <summary>
/// Genere le code C# d'un DbContext EF Core a partir du catalogue d'une base : une classe par
/// table ou vue, un DbSet par classe, et le mapping (tables, cles, noms de colonnes, precisions).
/// </summary>
public interface IEfModelGenerator
{
    EfGeneratedModel Generate(DatabaseSchemaSnapshot snapshot);
}
