using Querify.Core.Ioc;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Completion;
using Quill.Code.CSharp;

namespace Querify.Languages.Linq.Completion;

/// <inheritdoc cref="ILinqCompletionEngine"/>
[AutoRegister(As = typeof(ILinqCompletionEngine))]
public sealed class LinqCompletionEngine : ILinqCompletionEngine
{
    private readonly ILinqWorkspace _workspace;
    private readonly ICSharpLanguageService _language;

    public LinqCompletionEngine(ILinqWorkspace workspace, ICSharpLanguageService language)
    {
        _workspace = workspace;
        _language = language;
    }

    public Task<CompletionList> GetCompletionsAsync(LinqModelAssembly? model, string query, int caretOffset, IReadOnlyList<LinqParameter>? parameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _language.GetCompletionsAsync(_workspace.GetDocument(model, query, parameters), caretOffset, cancellationToken);
    }
}
