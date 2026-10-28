using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.Analysis;
using Quill.Code.Scripting;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Querify.Tests;

/// <summary>Requetes compilees contre le modele genere, traduites en SQL sans connexion.</summary>
public class LinqCompilerTests
{
    private const string ConnectionString = "Server=localhost;Database=RaveOnDb;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly Lazy<LinqModelAssembly> Model = new(() => LinqCompiler.CompileModelAsync(new EfModelGenerator().Generate(EfModelGeneratorTests.SampleSnapshot())).GetAwaiter().GetResult());

    [Fact]
    public async Task MethodSyntax_TranslatesToSql()
    {
        var sql = await TranslateAsync(TestLinq.Expression("""
            RaveOnDb.AspNetUsers
                .Where(u => u.Email != null && !u.EmailConfirmed)
                .OrderBy(u => u.Email)
                .Select(u => new { u.Id, u.Email })
            """));

        Assert.Contains("WHERE", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY", sql, StringComparison.Ordinal);
        Assert.Contains("[Email]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QuerySyntax_TranslatesToSql()
    {
        var sql = await TranslateAsync(TestLinq.Expression("from u in RaveOnDb.AspNetUsers where u.AccessFailedCount > 3 select u.Id"));

        Assert.Contains("[AccessFailedCount] > 3", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Join_OnCompositeKeyTable()
    {
        var sql = await TranslateAsync(TestLinq.Expression("""
            from u in RaveOnDb.AspNetUsers
            join r in RaveOnDb.AspNetUserRoles on u.Id equals r.UserId
            select new { u.Email, r.RoleId }
            """));

        Assert.Contains("INNER JOIN", sql, StringComparison.Ordinal);
        Assert.Contains("[AspNetUserRoles]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scalar_ReturnsValueType()
    {
        var compiled = await CompileAsync(TestLinq.Expression("40 + 2"));

        Assert.True(compiled.ReturnsValue);
        Assert.Equal(42, Run(compiled));
    }

    [Fact]
    public async Task Statements_ComposeBeforeReturn()
    {
        var sql = await TranslateAsync(TestLinq.Statements("""
            var counts = RaveOnDb.AspNetUsers.Select(u => u.AccessFailedCount);
            return counts.Where(i => i > 10);
            """));

        Assert.Contains("> 10", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Usings_DeclaredByTheQuery()
    {
        var compiled = await CompileAsync("""
            using System.Text;

            class UserQuery
            {
                object Run(QueryDbContext context) => new StringBuilder("a").Append('b').ToString();
            }
            """);

        Assert.Equal("ab", Run(compiled));
    }

    [Fact]
    public async Task Context_TypeNamedAfterDatabaseOrAlias()
    {
        var compiled = await CompileAsync("""
            static class UserQuery
            {
                static object Run(RaveOnDbContext context) => context is QueryDbContext;
            }
            """);

        Assert.Equal(true, Run(compiled));
    }

    [Fact]
    public async Task Void_ReturnsNothing()
    {
        var compiled = await CompileAsync("""
            class UserQuery
            {
                void Run(QueryDbContext context) => context.AspNetUsers.Dump();
            }
            """);

        Assert.False(compiled.ReturnsValue);
        Assert.Null(Run(compiled));
    }

    [Fact]
    public async Task Async_ResultIsAwaited()
    {
        var compiled = await CompileAsync("""
            class UserQuery
            {
                async Task<int> Run(QueryDbContext context)
                {
                    await Task.Yield();
                    return 42;
                }
            }
            """);

        Assert.True(compiled.ReturnsValue);
        Assert.Equal(42, Run(compiled));
    }

    [Fact]
    public async Task Parameters_BoundByNameAndConverted()
    {
        // Nom sans casse ni @, valeur convertie au type declare (int? du dashboard -> long).
        using var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, """
            class UserQuery
            {
                object Run(QueryDbContext context, long userId, int limit = 5) => userId * 2 + limit;
            }
            """, [new LinqParameter("@UserId", typeof(int?), 16)]);
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

        Assert.Equal(37L, Run(compiled));
    }

    [Fact]
    public async Task Parameters_SentAsSqlParameters()
    {
        using var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, """
            class UserQuery
            {
                object Run(QueryDbContext context, string email) => context.AspNetUsers.Where(u => u.Email == email);
            }
            """, [new LinqParameter("Email", typeof(string), "a@b.fr")]);
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

        var sql = Assert.IsAssignableFrom<IQueryable>(Run(compiled)).ToQueryString();

        // ToQueryString declare la valeur en tete, mais le WHERE compare bien a un parametre.
        Assert.Contains("= @", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Parameters_UnknownIsAnError()
    {
        var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, """
            class UserQuery
            {
                object Run(QueryDbContext context, int missing) => missing;
            }
            """);

        Assert.False(compiled.Success);
        var error = Assert.Single(compiled.Errors);
        Assert.Equal("LINQ001", error.Code);
        Assert.Contains("missing", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("class Other { object Run(QueryDbContext context) => 1; }")]
    [InlineData("class UserQuery { object Execute(QueryDbContext context) => 1; }")]
    [InlineData("class UserQuery { object Run() => 1; }")]
    [InlineData("class UserQuery { object Run(int value) => value; }")]
    public async Task EntryPoint_InvalidIsAnError(string query)
    {
        var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, query);

        Assert.False(compiled.Success);
        Assert.Equal("LINQ001", Assert.Single(compiled.Errors).Code);
    }

    [Fact]
    public async Task Errors_PointToQueryText()
    {
        var query = TestLinq.Statements("var users = RaveOnDb.AspNetUsers;\n        return users.Wher(u => true);");
        var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, query);

        Assert.False(compiled.Success);
        var error = compiled.Errors[0];
        Assert.Equal("CS1061", error.Code);
        Assert.Equal("Wher", query.Substring(error.Start, error.Length));
    }

    [Fact]
    public async Task Errors_UnknownContext()
    {
        var query = TestLinq.Expression("Northwind.Orders");
        var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, query);

        Assert.Contains(compiled.Errors, e => e.Code == "CS0103" && e.Start == query.IndexOf("Northwind", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dump_CollectsEachValueAndContinues()
    {
        var compiled = await CompileAsync(TestLinq.Statements("""
            RaveOnDb.AspNetUsers.Dump("Utilisateurs");
                    var total = 40.Dump();
                    return total + 2;
            """));

        List<(object? Value, string? Title)> dumps = [];
        using var context = LinqCompiler.CreateContext(Model.Value, ConnectionString);
        compiled.Context = context;
        compiled.Dump = (value, title) => dumps.Add((value, title));
        var result = compiled.Run();

        Assert.Equal(42, result);
        Assert.Equal(2, dumps.Count);
        Assert.IsAssignableFrom<IQueryable>(dumps[0].Value);
        Assert.Equal("Utilisateurs", dumps[0].Title);
        Assert.Equal(40, dumps[1].Value);
        Assert.Null(dumps[1].Title);
    }

    [Fact]
    public async Task Log_IsRedirectedWithItsLevel()
    {
        List<(LogLevel Level, string Text)> output = [];
        var compiled = await CompileAsync(TestLinq.Statements("""
            Log.Information("a");
                    Log.Warning($"{1}+{2}");
                    Log.Write(LogLevel.Error, "{b}");
                    Log.Write(LogLevel.None, "ignore");
                    return 0;
            """), (level, text) => output.Add((level, text)));

        Run(compiled);

        Assert.Equal(new[] { (LogLevel.Information, "a"), (LogLevel.Warning, "1+2"), (LogLevel.Error, "{b}") }, output);
    }

    private static async Task<string> TranslateAsync(string query)
    {
        var compiled = await CompileAsync(query);
        return Assert.IsAssignableFrom<IQueryable>(Run(compiled)).ToQueryString();
    }

    /// <summary>
    /// Execute sans ouvrir de connexion : une requete IQueryable n'est que traduite (ToQueryString), apres coup,
    /// d'ou un contexte non libere ici.
    /// </summary>
    private static object? Run(LinqCompiledQuery compiled)
    {
        compiled.Context = LinqCompiler.CreateContext(Model.Value, ConnectionString);
        return compiled.Run();
    }

    private static async Task<LinqCompiledQuery> CompileAsync(string query, Action<LogLevel, string>? log = null)
    {
        var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, query, log: log);
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Errors));
        return compiled;
    }
}
