using Querify.Languages.Linq.Queries;
using Querify.Languages.Sql.Queries;
using System.Reflection;

namespace Querify.Languages;

/// <summary>Assemblies des langages portant des classes [AutoRegister], a scanner par l'application.</summary>
public static class LanguageAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(LanguageAssemblies).Assembly,
        typeof(SqlQueryExecutor).Assembly,
        typeof(LinqQueryExecutor).Assembly,
    ];
}
