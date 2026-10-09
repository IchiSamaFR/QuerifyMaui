using SqlToolManagerMaui.Languages.Linq;
using SqlToolManagerMaui.Languages.Sql.Queries;
using System.Reflection;

namespace SqlToolManagerMaui.Languages;

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
