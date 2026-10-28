using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Modeling;
using Quill.Code.Completion;
using Xunit;

namespace Querify.Tests;

/// <summary>Le caractere | marque le curseur ; Roslyn complete dans le contexte genere.</summary>
public class LinqCompletionTests
{
    private static readonly Lazy<LinqModelAssembly> Model = new(() => LinqCompiler.CompileModelAsync(new EfModelGenerator().Generate(EfModelGeneratorTests.SampleSnapshot())).GetAwaiter().GetResult());

    [Fact]
    public async Task Context_ListsTables()
    {
        var items = await CompleteAsync(TestLinq.Expression("RaveOnDb.|"));

        Assert.Contains(items, i => i.InsertText == "AspNetUsers" && i.Kind == CompletionItemKind.Property);
        Assert.Contains(items, i => i.InsertText == "Sales_Orders");
    }

    [Fact]
    public async Task Parameters_OfRunAreListed()
    {
        var items = await CompleteAsync("""
            class UserQuery
            {
                object Run(QueryDbContext context, int maxFailures) => context.AspNetUsers.Where(u => u.AccessFailedCount > |);
            }
            """);

        Assert.Contains(items, i => i.InsertText == "maxFailures");
    }

    [Fact]
    public async Task Usings_DeclaredByTheQuery()
    {
        var items = await CompleteAsync("""
            using System.Text;

            class UserQuery
            {
                object Run(QueryDbContext context) => new StringBu|
            }
            """);

        Assert.Contains(items, i => i.InsertText == "StringBuilder");
    }

    [Fact]
    public async Task Lambda_ListsColumns()
    {
        var items = await CompleteAsync(TestLinq.Expression("RaveOnDb.AspNetUsers.Where(u => u.|)"));

        Assert.Contains(items, i => i.InsertText == "Email" && i.Kind == CompletionItemKind.Property);
        Assert.Contains(items, i => i.InsertText == "EmailConfirmed");
    }

    [Fact]
    public async Task Queryable_ListsLinqOperators()
    {
        var items = await CompleteAsync(TestLinq.Expression("RaveOnDb.AspNetUsers.Wh|"));

        Assert.Contains(items, i => i.InsertText == "Where" && i.Kind is CompletionItemKind.Method or CompletionItemKind.ExtensionMethod);
    }

    [Fact]
    public async Task QuerySyntax_RangeVariableMembers()
    {
        var items = await CompleteAsync(TestLinq.Expression("from u in RaveOnDb.AspNetUsers where u.|"));

        Assert.Contains(items, i => i.InsertText == "AccessFailedCount");
    }

    [Fact]
    public async Task Statements_LocalVariableMembers()
    {
        var items = await CompleteAsync(TestLinq.Statements("var ids = RaveOnDb.AspNetUsers.Select(u => u.Id);\nreturn ids.Wh|"));

        Assert.Contains(items, i => i.InsertText == "Where");
    }

    [Fact]
    public async Task Statements_DeclarationInProgress()
    {
        var items = await CompleteAsync(TestLinq.Statements("var users = RaveOnDb.|"));

        Assert.Contains(items, i => i.InsertText == "AspNetUsers");
    }

    [Fact]
    public async Task WithoutModel_StillCompletesCSharp()
    {
        var query = TestLinq.Expression("DateTime.");
        var items = await TestLinq.CompleteAsync(null, query, query.IndexOf("DateTime.", StringComparison.Ordinal) + "DateTime.".Length);

        Assert.Contains(items, i => i.InsertText == "Now");
    }

    private static async Task<IReadOnlyList<CompletionItem>> CompleteAsync(string queryWithCaret)
    {
        var caret = queryWithCaret.IndexOf('|', StringComparison.Ordinal);
        return await TestLinq.CompleteAsync(Model.Value, queryWithCaret.Remove(caret, 1), caret);
    }
}
