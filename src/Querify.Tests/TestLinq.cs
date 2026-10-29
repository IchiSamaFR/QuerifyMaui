using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Quill.Code.Analysis;
using Quill.Code.Completion;

namespace Querify.Tests;

/// <summary>
/// Analyse d'une requete dans son contexte genere, comme le fait l'editeur LINQ une fois le DbContext resolu
/// (services de <see cref="LinqCompiler"/>, partages : la composition MEF est couteuse).
/// </summary>
internal static class TestLinq
{
    /// <summary>Requete C# native dont <paramref name="expression"/> est la valeur d'une variable ; le contexte s'appelle Db.</summary>
    public static string Expression(string expression) => $"var result = {expression};\n";

    /// <summary>Requete C# native faite des instructions <paramref name="statements"/>.</summary>
    public static string Statements(string statements) => statements + "\n";

    public static async Task<IReadOnlyList<CompletionItem>> CompleteAsync(LinqGeneratedModelAssembly? model, string query, int caret, IReadOnlyList<LinqParameter>? parameters = null)
        => (await LinqCompiler.GetLanguageService(model, parameters).GetCompletionsAsync(query, caret)).Items;

    public static Task<HoverInfo?> HoverAsync(LinqGeneratedModelAssembly? model, string query, int offset)
        => LinqCompiler.GetLanguageService(model).GetHoverAsync(query, offset);

    public static Task<IReadOnlyList<CodeDiagnostic>> DiagnosticsAsync(LinqGeneratedModelAssembly? model, string query)
        => LinqCompiler.GetLanguageService(model).GetDiagnosticsAsync(query);

    public static Task<SignatureHelpInfo?> SignatureHelpAsync(LinqGeneratedModelAssembly? model, string query, int offset)
        => LinqCompiler.GetLanguageService(model).GetSignatureHelpAsync(query, offset);
}
