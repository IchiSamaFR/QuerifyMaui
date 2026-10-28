using Querify.Languages.Linq.Compilation;
using Quill.Code.Analysis;
using Quill.Code.Completion;

namespace Querify.Tests;

/// <summary>
/// Analyse d'une requete dans son contexte genere, comme le fait l'editeur LINQ une fois le DbContext resolu
/// (services de <see cref="LinqCompiler"/>, partages : la composition MEF est couteuse).
/// </summary>
internal static class TestLinq
{
    /// <summary>Requete complete renvoyant <paramref name="expression"/> ; le contexte s'appelle RaveOnDb, comme la base de test.</summary>
    public static string Expression(string expression)
        => $"class UserQuery\n{{\n    object Run(QueryDbContext RaveOnDb) =>\n        {expression};\n}}\n";

    /// <summary>Requete complete dont <paramref name="statements"/> forment le corps de Run.</summary>
    public static string Statements(string statements)
        => $"class UserQuery\n{{\n    object Run(QueryDbContext RaveOnDb)\n    {{\n        {statements}\n    }}\n}}\n";

    public static async Task<IReadOnlyList<CompletionItem>> CompleteAsync(LinqModelAssembly? model, string query, int caret)
        => (await LinqCompiler.GetLanguageService(model).GetCompletionsAsync(query, caret)).Items;

    public static Task<HoverInfo?> HoverAsync(LinqModelAssembly? model, string query, int offset)
        => LinqCompiler.GetLanguageService(model).GetHoverAsync(query, offset);

    public static Task<IReadOnlyList<CodeDiagnostic>> DiagnosticsAsync(LinqModelAssembly? model, string query)
        => LinqCompiler.GetLanguageService(model).GetDiagnosticsAsync(query);

    public static Task<SignatureHelpInfo?> SignatureHelpAsync(LinqModelAssembly? model, string query, int offset)
        => LinqCompiler.GetLanguageService(model).GetSignatureHelpAsync(query, offset);
}
