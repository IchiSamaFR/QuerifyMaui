using Querify.Databases.Models.Connections;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Querify.Languages.Linq.Workspaces;
using Quill.Code;
using Quill.Code.Analysis;
using Quill.Code.Completion;
using Quill.Code.CSharp;
using Quill.Code.Languages;

namespace Querify.Languages.Linq.Editors;

/// <summary>
/// Service d'un editeur LINQ : relaie au service C# du contexte, resolu a chaque demande (avec le DbContext de la base
/// des que la connexion est ouverte, C# seul sinon).
/// </summary>
internal sealed class LinqEditorService : ICodeLanguageService
{
    private readonly ILinqContexts _contexts;
    private readonly DatabaseConnectionInfo? _connection;
    private readonly string? _database;
    private readonly IReadOnlyList<LinqParameter> _parameters;

    public LinqEditorService(ILinqContexts contexts, DatabaseConnectionInfo? connection, string? database, IReadOnlyList<LinqParameter> parameters)
    {
        _contexts = contexts;
        _connection = connection;
        _database = database;
        _parameters = parameters;
    }

    public string Language => CodeLanguages.CSharp;

    public async Task<CompletionList> GetCompletionsAsync(string code, int position, CancellationToken cancellationToken = default)
        => await (await GetServiceAsync(cancellationToken)).GetCompletionsAsync(code, position, cancellationToken);

    public async Task<CompletionChange> GetChangeAsync(string code, CompletionItem item, CancellationToken cancellationToken = default)
        => await (await GetServiceAsync(cancellationToken)).GetChangeAsync(code, item, cancellationToken);

    /// <summary>Les informations (suggestions) ne sont pas soulignees, ni l'absence d'instruction (signalee a l'execution).</summary>
    public async Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(string code, CancellationToken cancellationToken = default)
    {
        var diagnostics = await (await GetServiceAsync(cancellationToken)).GetDiagnosticsAsync(code, cancellationToken);
        return diagnostics.Where(d => d.Severity != CodeDiagnosticSeverity.Info && d.Code != LinqQuerySource.NoEntryPointCode).ToArray();
    }

    public async Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default)
        => await (await GetServiceAsync(cancellationToken)).GetHoverAsync(code, position, cancellationToken);

    public async Task<SignatureHelpInfo?> GetSignatureHelpAsync(string code, int position, CancellationToken cancellationToken = default)
        => await (await GetServiceAsync(cancellationToken)).GetSignatureHelpAsync(code, position, cancellationToken);

    private Task<CSharpLanguageService> GetServiceAsync(CancellationToken cancellationToken)
        => _contexts.GetLanguageServiceAsync(_connection, _database, _parameters, cancellationToken);
}
