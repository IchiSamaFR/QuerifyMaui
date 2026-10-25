using SqlToolManagerMaui.Languages.Linq.Compilation;
using SqlToolManagerMaui.Languages.Linq.Completion;
using SqlToolManagerMaui.Languages.Linq.Modeling;
using SqlToolManagerMaui.Languages.Linq.Queries;
using SqlToolManagerMaui.Languages.Linq.Workspaces;
using Xunit;

namespace SqlToolManagerMaui.Tests;

/// <summary>Le caractere | marque le curseur ; Roslyn complete dans le contexte genere.</summary>
public class LinqCompletionEngineTests
{
    private static readonly LinqCompletionEngine Engine = new(new LinqWorkspace());

    private static readonly Lazy<LinqModelAssembly> Model = new(() => new LinqCompiler(new LinqWorkspace()).CompileModel(new EfModelGenerator().Generate(EfModelGeneratorTests.SampleSnapshot())));

    [Fact]
    public async Task Context_ListsTables()
    {
        var items = await CompleteAsync("RaveOnDb.|");

        Assert.Contains(items, i => i.InsertText == "AspNetUsers" && i.Kind == "Property");
        Assert.Contains(items, i => i.InsertText == "Sales_Orders");
    }

    [Fact]
    public async Task Parameters_ListedAsVariables()
    {
        const string query = "RaveOnDb.AspNetUsers.Where(u => u.AccessFailedCount > |)";
        var caret = query.IndexOf('|', StringComparison.Ordinal);

        var items = await Engine.GetCompletionsAsync(Model.Value, query.Remove(caret, 1), caret, [new LinqParameter("MaxFailures", typeof(int?), null)]);

        Assert.Contains(items, i => i.InsertText == "MaxFailures" && i.Kind == "Local");
    }

    [Fact]
    public async Task Lambda_ListsColumns()
    {
        var items = await CompleteAsync("RaveOnDb.AspNetUsers.Where(u => u.|)");

        Assert.Contains(items, i => i.InsertText == "Email" && i.Kind == "Property");
        Assert.Contains(items, i => i.InsertText == "EmailConfirmed");
    }

    [Fact]
    public async Task Queryable_ListsLinqOperators()
    {
        var items = await CompleteAsync("RaveOnDb.AspNetUsers.Wh|");

        Assert.Contains(items, i => i.InsertText == "Where" && i.Kind is "Method" or "ExtensionMethod");
    }

    [Fact]
    public async Task QuerySyntax_RangeVariableMembers()
    {
        var items = await CompleteAsync("from u in RaveOnDb.AspNetUsers where u.|");

        Assert.Contains(items, i => i.InsertText == "AccessFailedCount");
    }

    [Fact]
    public async Task Statements_LocalVariableMembers()
    {
        var items = await CompleteAsync("var ids = RaveOnDb.AspNetUsers.Select(u => u.Id);\nreturn ids.Wh|");

        Assert.Contains(items, i => i.InsertText == "Where");
    }

    [Fact]
    public async Task Statements_DeclarationInProgress()
    {
        var items = await CompleteAsync("var users = RaveOnDb.|");

        Assert.Contains(items, i => i.InsertText == "AspNetUsers");
    }

    [Fact]
    public async Task WithoutModel_StillCompletesCSharp()
    {
        var caret = "DateTime.".Length;
        var items = await Engine.GetCompletionsAsync(null, "DateTime.", caret);

        Assert.Contains(items, i => i.InsertText == "Now");
    }

    [Fact]
    public async Task StartOfExpression_OnlyQueryUsefulItems()
    {
        var items = await CompleteAsync("|");

        Assert.Contains(items, i => i.InsertText == "RaveOnDb");
        Assert.Contains(items, i => i.InsertText == "new" && i.Kind == "Keyword");
        Assert.Contains(items, i => i.InsertText == "DateTime");
        Assert.DoesNotContain(items, i => i.Kind == "Namespace");
        Assert.DoesNotContain(items, i => i.InsertText is "System" or "__context" or "__dump" or "UserQuery");
    }

    [Fact]
    public async Task Context_HidesDbContextPlumbing()
    {
        var items = await CompleteAsync("RaveOnDb.|");

        Assert.Contains(items, i => i.InsertText == "AspNetUsers");
        Assert.DoesNotContain(items, i => i.InsertText is "SaveChanges" or "ChangeTracker" or "Database" or "GetHashCode");
    }

    [Fact]
    public async Task Queryable_HidesAsyncAndObjectMembers()
    {
        var items = await CompleteAsync("RaveOnDb.AspNetUsers.|");

        Assert.Contains(items, i => i.InsertText == "Where");
        Assert.DoesNotContain(items, i => i.InsertText is "ToListAsync" or "Equals" or "GetType");
    }

    private static Task<IReadOnlyList<LinqCompletionCandidate>> CompleteAsync(string queryWithCaret)
    {
        var caret = queryWithCaret.IndexOf('|', StringComparison.Ordinal);
        return Engine.GetCompletionsAsync(Model.Value, queryWithCaret.Remove(caret, 1), caret);
    }
}
