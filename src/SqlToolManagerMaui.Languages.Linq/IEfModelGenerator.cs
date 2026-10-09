using SqlToolManagerMaui.Core.Models.Databases.Schemas;

namespace SqlToolManagerMaui.Languages.Linq;

/// <summary>
/// Genere le code C# d'un DbContext EF Core a partir du catalogue d'une base : une classe par
/// table ou vue, un DbSet par classe, et le mapping (tables, cles, noms de colonnes, precisions).
/// </summary>
public interface IEfModelGenerator
{
    EfGeneratedModel Generate(DatabaseSchemaSnapshot snapshot);
}

/// <param name="Namespace">Espace de noms du code genere.</param>
/// <param name="ContextClassName">Classe du DbContext (ex. RaveOnDbContext).</param>
/// <param name="ContextVariableName">Nom sous lequel la requete designe le contexte : celui de la base (ex. RaveOnDb).</param>
/// <param name="Source">Code C# complet, compilable seul avec EF Core.</param>
/// <param name="EntityClassNames">Classes generees, dans l'ordre du source.</param>
public sealed record EfGeneratedModel(
    string Namespace,
    string ContextClassName,
    string ContextVariableName,
    string Source,
    IReadOnlyList<string> EntityClassNames);
