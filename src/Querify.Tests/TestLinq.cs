using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Workspaces;

namespace Querify.Tests;

/// <summary>
/// Workspace et services Roslyn partages par les tests (la composition MEF est couteuse), et analyse d'une requete
/// dans son contexte genere, comme le font les fournisseurs LINQ une fois le DbContext resolu.
/// </summary>
internal static class TestLinq
{
    public static LinqWorkspace Workspace { get; } = new(new CSharpWorkspace());

    public static CSharpLanguageService Language { get; } = new();

    public static async Task<IReadOnlyList<CompletionItem>> CompleteAsync(LinqModelAssembly? model, string query, int caret, IReadOnlyList<LinqParameter>? parameters = null)
        => (await Language.GetCompletionsAsync(Workspace.GetDocument(model, query, parameters), caret)).Items;

    public static Task<HoverInfo?> HoverAsync(LinqModelAssembly? model, string query, int offset)
        => Language.GetHoverAsync(Workspace.GetDocument(model, query), offset);

    public static Task<IReadOnlyList<CodeDiagnostic>> DiagnosticsAsync(LinqModelAssembly? model, string query)
        => Language.GetDiagnosticsAsync(Workspace.GetDocument(model, query));

    public static Task<SignatureHelpInfo?> SignatureHelpAsync(LinqModelAssembly? model, string query, int offset)
        => Language.GetSignatureHelpAsync(Workspace.GetDocument(model, query), offset);
}
