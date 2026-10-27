using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.Completion;

namespace Querify.Tests;

/// <summary>
/// Services Roslyn partages par les tests (la composition MEF est couteuse), et analyse d'une requete dans son
/// contexte genere, comme le font les fournisseurs LINQ une fois le DbContext resolu.
/// </summary>
internal static class TestLinq
{
    public static LinqLanguageServices Services { get; } = new();

    public static async Task<IReadOnlyList<CompletionItem>> CompleteAsync(LinqModelAssembly? model, string query, int caret, IReadOnlyList<LinqParameter>? parameters = null)
        => (await Services.Get(model, parameters).GetCompletionsAsync(query, caret)).Items;

    public static Task<HoverInfo?> HoverAsync(LinqModelAssembly? model, string query, int offset)
        => Services.Get(model).GetHoverAsync(query, offset);

    public static Task<IReadOnlyList<CodeDiagnostic>> DiagnosticsAsync(LinqModelAssembly? model, string query)
        => Services.Get(model).GetDiagnosticsAsync(query);

    public static Task<SignatureHelpInfo?> SignatureHelpAsync(LinqModelAssembly? model, string query, int offset)
        => Services.Get(model).GetSignatureHelpAsync(query, offset);
}
