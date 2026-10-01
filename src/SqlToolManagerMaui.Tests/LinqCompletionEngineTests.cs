using SqlToolManagerMaui.Services.Linq;
using Xunit;

namespace SqlToolManagerMaui.Tests;

/// <summary>Le caractere | marque le curseur ; Roslyn complete dans le contexte genere.</summary>
public class LinqCompletionEngineTests
{
    private static readonly LinqCompletionEngine Engine = new();

    private static readonly Lazy<LinqModelAssembly> Model = new(() => new LinqCompiler().CompileModel(new EfModelGenerator().Generate(EfModelGeneratorTests.SampleSnapshot())));

    [Fact]
    public async Task Context_ListsTables()
    {
        var items = await CompleteAsync("RaveOnDb.|");

        Assert.Contains(items, i => i.InsertText == "AspNetUsers" && i.Kind == "Property");
        Assert.Contains(items, i => i.InsertText == "Sales_Orders");
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
    public async Task WithoutModel_StillCompletesCSharp()
    {
        var caret = "DateTime.".Length;
        var items = await Engine.GetCompletionsAsync(null, "DateTime.", caret);

        Assert.Contains(items, i => i.InsertText == "Now");
    }

    private static Task<IReadOnlyList<LinqCompletionCandidate>> CompleteAsync(string queryWithCaret)
    {
        var caret = queryWithCaret.IndexOf('|', StringComparison.Ordinal);
        return Engine.GetCompletionsAsync(Model.Value, queryWithCaret.Remove(caret, 1), caret);
    }
}
