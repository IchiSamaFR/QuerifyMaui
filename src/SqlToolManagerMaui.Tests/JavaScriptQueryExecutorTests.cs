using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Queries;
using SqlToolManagerMaui.Languages.Commons.Queries;
using SqlToolManagerMaui.Languages.JavaScript;
using System.Diagnostics;
using Xunit;

namespace SqlToolManagerMaui.Tests;

/// <summary>Scripts JavaScript et TypeScript executes sans base : db n'est appele que dans un test dedie.</summary>
public class JavaScriptQueryExecutorTests
{
    private static readonly DatabaseConnectionInfo Connection = new() { Name = "Test" };

    private static readonly SucraseTypeScriptTranspiler Transpiler = new();

    [Fact]
    public async Task ReturnedArray_BecomesResultSet_WithUnionOfKeys()
    {
        var result = await RunJavaScriptAsync("""
            const rows = [{ id: 1, name: "a" }, { id: 2, active: true }];
            return rows;
            """);

        Assert.False(result.HasErrors, Errors(result));
        var set = Assert.Single(result.ResultSets);
        Assert.Equal(new[] { "id", "name", "active" }, set.Columns.Select(c => c.Name));
        Assert.Equal(new string?[] { "2", null, "1" }, set.Rows[1].Cells);
    }

    [Fact]
    public async Task Dump_AddsResultSets_AndReturnedDumpIsNotDuplicated()
    {
        var result = await RunJavaScriptAsync("""
            [1, 2, 3].dump("Nombres");
            const total = { total: 6 };
            return total.dump();
            """);

        Assert.False(result.HasErrors, Errors(result));
        Assert.Equal(2, result.ResultSets.Count);
        Assert.Equal("Nombres", result.ResultSets[0].Title);
        Assert.Equal(3, result.ResultSets[0].Rows.Count);
        Assert.Equal("6", result.ResultSets[1].Rows[0].Cells[0]);
    }

    [Fact]
    public async Task FinalExpression_IsAResult_ButNotADeclaration()
    {
        var expression = await RunJavaScriptAsync("const n = 40;\nn + 2");
        var declaration = await RunJavaScriptAsync("const n = 40;\nlet m = n + 2;");

        Assert.Equal("42", Assert.Single(expression.ResultSets).Rows[0].Cells[0]);
        Assert.Empty(declaration.ResultSets);
    }

    [Fact]
    public async Task ConsoleLog_GoesToMessages()
    {
        var result = await RunJavaScriptAsync("console.log('bonjour', { a: 1 });");

        Assert.Contains(result.Messages, m => m.Text == "bonjour {\"a\":1}");
    }

    [Fact]
    public async Task TypeScript_TypesAreStripped_BeforeExecution()
    {
        var result = await RunTypeScriptAsync("""
            interface Row { id: number; label: string }
            enum Kind { A, B }
            function first<T>(items: T[]): T { return items[0]; }
            const rows: Row[] = [{ id: Kind.B, label: "b" } as Row];
            return first<Row>(rows);
            """);

        Assert.False(result.HasErrors, Errors(result));
        var set = Assert.Single(result.ResultSets);
        Assert.Equal(new string?[] { "1", "b" }, set.Rows[0].Cells);
    }

    [Fact]
    public void Transpile_KeepsLineNumbers()
    {
        var javaScript = Transpiler.Transpile("type A = { x: number };\nconst a: A = { x: 1 };\nreturn a;");

        Assert.Equal(3, javaScript.ReplaceLineEndings("\n").Split('\n').Length);
        Assert.DoesNotContain(": A", javaScript, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InfiniteLoop_IsInterruptedByTimeout()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await RunJavaScriptAsync("while (true) { }", TimeSpan.FromSeconds(1));

        Assert.True(result.HasErrors);
        Assert.Contains(result.Messages, m => m.IsError && m.Text.Contains("Délai d'exécution dépassé", StringComparison.Ordinal));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task InfiniteLoop_IsInterruptedByCancellation()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var result = await CreateRunner(TimeSpan.Zero).ExecuteAsync(Connection, "for (;;) { }", transform: null, cancellation.Token);

        Assert.True(result.WasCancelled);
    }

    [Fact]
    public async Task SyntaxError_IsReportedWithLineInMessages()
    {
        var result = await RunJavaScriptAsync("const a = 1;\nconst = 2;");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Messages, m => m.IsError && m.Text.StartsWith("Ligne 2, colonne", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TypeScriptSyntaxError_IsReportedWithLineInMessages()
    {
        var result = await RunTypeScriptAsync("const a: number = 1;\nconst b: = 2;");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Messages, m => m.IsError && m.Text.StartsWith("Ligne 2, colonne", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RuntimeError_IsReportedInMessages()
    {
        var result = await RunJavaScriptAsync("const a = null;\nreturn a.b;");

        Assert.True(result.HasErrors);
        Assert.Contains(result.Messages, m => m.IsError && m.Text.Contains("TypeError", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DbQuery_WithoutOpenConnection_RaisesCatchableError()
    {
        var result = await RunJavaScriptAsync("""
            try { db.query("select 1"); }
            catch (e) { return e.message; }
            """);

        Assert.Contains("n'est pas ouverte", Assert.Single(result.ResultSets).Rows[0].Cells[0] ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadAsync_IsNotSupported()
    {
        var executor = new JavaScriptQueryExecutor(CreateRunner(TimeSpan.Zero));

        await Assert.ThrowsAsync<NotSupportedException>(() => executor.ReadAsync(Connection, null, "return 1", [], 10));
    }

    private static Task<QueryExecutionResult> RunJavaScriptAsync(string script, TimeSpan? timeout = null)
        => new JavaScriptQueryExecutor(CreateRunner(timeout ?? TimeSpan.FromSeconds(30))).ExecuteAsync(Connection, script);

    private static Task<QueryExecutionResult> RunTypeScriptAsync(string script)
        => new TypeScriptQueryExecutor(CreateRunner(TimeSpan.FromSeconds(30)), Transpiler).ExecuteAsync(Connection, script);

    private static JavaScriptRunner CreateRunner(TimeSpan timeout) => new(new QueryReaderResolver([]), new FixedSettings(timeout));

    private static string Errors(QueryExecutionResult result)
        => string.Join(Environment.NewLine, result.Messages.Where(m => m.IsError).Select(m => m.Text));

    private sealed class FixedSettings(TimeSpan timeout) : IScriptExecutionSettings
    {
        public TimeSpan ScriptTimeout => timeout;
    }
}
