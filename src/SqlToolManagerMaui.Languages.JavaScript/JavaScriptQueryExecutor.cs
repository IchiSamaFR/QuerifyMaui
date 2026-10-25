using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;
using SqlToolManagerMaui.Languages.Commons.Queries;

namespace SqlToolManagerMaui.Languages.JavaScript;

/// <summary>Script JavaScript execute par <see cref="IJavaScriptRunner"/>, avec l'objet db sur la connexion.</summary>
[AutoRegister(As = typeof(IQueryExecutor))]
public sealed class JavaScriptQueryExecutor : IQueryExecutor
{
    private readonly IJavaScriptRunner _runner;

    public JavaScriptQueryExecutor(IJavaScriptRunner runner)
    {
        _runner = runner;
    }

    public SyntaxLanguage Language => SyntaxLanguage.JavaScript;

    public Task<QueryExecutionResult> ExecuteAsync(DatabaseConnectionInfo connection, string script, CancellationToken cancellationToken = default)
        => _runner.ExecuteAsync(connection, script, transform: null, cancellationToken);

    public Task<QueryResultSet> ReadAsync(DatabaseConnectionInfo connection, string? database, string query, IReadOnlyList<QueryParameter> parameters, int maxRows, CancellationToken cancellationToken = default)
        => Task.FromException<QueryResultSet>(new NotSupportedException("Les cartes du dashboard ne prennent pas en charge JavaScript : utilisez SQL ou LINQ."));
}
