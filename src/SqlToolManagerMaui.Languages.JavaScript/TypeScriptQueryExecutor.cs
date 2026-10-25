using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;
using SqlToolManagerMaui.Languages.Commons.Queries;

namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Script TypeScript : types retires (sans verification), puis execute comme du JavaScript.</summary>
[AutoRegister(As = typeof(IQueryExecutor))]
public sealed class TypeScriptQueryExecutor : IQueryExecutor
{
    private readonly IJavaScriptRunner _runner;
    private readonly ITypeScriptTranspiler _transpiler;

    public TypeScriptQueryExecutor(IJavaScriptRunner runner, ITypeScriptTranspiler transpiler)
    {
        _runner = runner;
        _transpiler = transpiler;
    }

    public SyntaxLanguage Language => SyntaxLanguage.TypeScript;

    public Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, CancellationToken cancellationToken = default)
        => _runner.ExecuteAsync(connection, script, _transpiler.Transpile, cancellationToken);

    public Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
        => Task.FromException<QueryResultSet>(new NotSupportedException("Les cartes du dashboard ne prennent pas en charge TypeScript : utilisez SQL ou LINQ."));
}
