using SqlToolManagerMaui.Languages.Linq.Compilation;
using SqlToolManagerMaui.Languages.Linq.Modeling;
using SqlToolManagerMaui.Languages.Linq.Queries;
using SqlToolManagerMaui.Languages.Linq.Workspaces;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SqlToolManagerMaui.Tests;

/// <summary>Requetes compilees contre le modele genere, traduites en SQL sans connexion.</summary>
public class LinqCompilerTests
{
    private const string ConnectionString = "Server=localhost;Database=RaveOnDb;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly LinqCompiler Compiler = new(new LinqWorkspace());

    private static readonly Lazy<LinqModelAssembly> Model = new(() => Compiler.CompileModel(new EfModelGenerator().Generate(EfModelGeneratorTests.SampleSnapshot())));

    [Fact]
    public async Task MethodSyntax_TranslatesToSql()
    {
        var sql = await TranslateAsync("""
            RaveOnDb.AspNetUsers
                .Where(u => u.Email != null && !u.EmailConfirmed)
                .OrderBy(u => u.Email)
                .Select(u => new { u.Id, u.Email });
            """);

        Assert.Contains("WHERE", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY", sql, StringComparison.Ordinal);
        Assert.Contains("[Email]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QuerySyntax_TranslatesToSql()
    {
        var sql = await TranslateAsync("from u in RaveOnDb.AspNetUsers where u.AccessFailedCount > 3 select u.Id");

        Assert.Contains("[AccessFailedCount] > 3", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Join_OnCompositeKeyTable()
    {
        var sql = await TranslateAsync("""
            from u in RaveOnDb.AspNetUsers
            join r in RaveOnDb.AspNetUserRoles on u.Id equals r.UserId
            select new { u.Email, r.RoleId }
            """);

        Assert.Contains("INNER JOIN", sql, StringComparison.Ordinal);
        Assert.Contains("[AspNetUserRoles]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scalar_ReturnsValueType()
    {
        var compiled = await CompileAsync("40 + 2");

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        Assert.Equal(42, compiled.Run!(context));
    }

    [Fact]
    public async Task Parameters_DeclaredAsTypedVariables()
    {
        // Nom seul ou @Nom (identifiant verbatim) : meme variable, comme un parametre SQL.
        using var compiled = await Compiler.CompileQueryAsync(Model.Value, "UserId + @UserId", [new LinqParameter("UserId", typeof(int?), 21)]);
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        Assert.Equal(42, compiled.Run!(context));
    }

    [Fact]
    public async Task Parameters_SentAsSqlParameters()
    {
        using var compiled = await Compiler.CompileQueryAsync(Model.Value, "RaveOnDb.AspNetUsers.Where(u => u.Email == @Email)",
            [new LinqParameter("Email", typeof(string), "a@b.fr")]);
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        var sql = Assert.IsAssignableFrom<IQueryable>(compiled.Run!(context)).ToQueryString();

        // ToQueryString declare la valeur en tete, mais le WHERE compare bien a un parametre.
        Assert.Contains("= @", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Statements_ReturnVariable()
    {
        var sql = await TranslateAsync("""
            var ids = RaveOnDb.AspNetUsers.Select(u => u.Id);
            return ids;
            """);

        Assert.Contains("[Id]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Statements_ComposeBeforeReturn()
    {
        var sql = await TranslateAsync("""
            var counts = RaveOnDb.AspNetUsers.Select(u => u.AccessFailedCount);
            return counts.Where(i => i > 10);
            """);

        Assert.Contains("> 10", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Statements_WithoutReturnGiveNull()
    {
        var compiled = await CompileAsync("var ids = RaveOnDb.AspNetUsers.Select(u => u.Id);");

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        Assert.Null(compiled.Run!(context));
    }

    [Fact]
    public async Task Statements_ErrorsPointToQueryText()
    {
        const string query = "var users = RaveOnDb.AspNetUsers;\nreturn users.Wher(u => true);";
        var compiled = await Compiler.CompileQueryAsync(Model.Value, query);

        Assert.False(compiled.Success);
        Assert.Equal(query.IndexOf("Wher", StringComparison.Ordinal), compiled.Errors[0].Start);
    }

    [Theory]
    [InlineData("RaveOnDb.AspNetUsers", false)]
    [InlineData("RaveOnDb.AspNetUsers;", false)]
    [InlineData("RaveOnDb.AspNetUsers.Where(u => u.Email == \"a;b\")", false)]
    [InlineData("return RaveOnDb.AspNetUsers;", true)]
    [InlineData("var ids = RaveOnDb.AspNetUsers", true)]
    [InlineData("var a = 1; a", true)]
    public void StatementDetection(string query, bool expected)
        => Assert.Equal(expected, LinqCompiler.IsStatementBlock(query));

    [Fact]
    public async Task Errors_PointToQueryText()
    {
        const string query = "RaveOnDb.AspNetUsers\n    .Wher(u => true)";
        var compiled = await Compiler.CompileQueryAsync(Model.Value, query);

        Assert.False(compiled.Success);
        var error = compiled.Errors[0];
        Assert.Equal("CS1061", error.Code);
        Assert.StartsWith("Ligne 2, colonne 6 : CS1061 ", LinqDiagnosticMapper.Describe(error, query), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Errors_UnknownContext()
    {
        var compiled = await Compiler.CompileQueryAsync(Model.Value, "Northwind.Orders");

        Assert.Contains(compiled.Errors, e => e.Code == "CS0103" && e.Start == 0);
    }

    [Fact]
    public async Task Dump_CollectsEachValueAndContinues()
    {
        var compiled = await CompileAsync("""
            RaveOnDb.AspNetUsers.Dump("Utilisateurs");
            var total = 40.Dump();
            return total + 2;
            """);

        List<(object? Value, string? Title)> dumps = [];
        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        var result = compiled.Execute!(context, (value, title) => dumps.Add((value, title)), null);

        Assert.Equal(42, result);
        Assert.Equal(2, dumps.Count);
        Assert.IsAssignableFrom<IQueryable>(dumps[0].Value);
        Assert.Equal("Utilisateurs", dumps[0].Title);
        Assert.Equal(40, dumps[1].Value);
        Assert.Null(dumps[1].Title);
    }

    [Fact]
    public async Task Console_IsRedirectedToTheWriter()
    {
        var compiled = await CompileAsync("""
            Console.Write("a");
            Console.WriteLine("{b}");
            Console.WriteLine("{0}+{1}", 1, 2);
            return 0;
            """);

        var output = new StringBuilder();
        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        compiled.Execute!(context, null, text => output.Append(text));

        Assert.Equal($"a{{b}}{Environment.NewLine}1+2{Environment.NewLine}", output.ToString());
    }

    [Theory]
    [InlineData("RaveOnDb.AspNetUsers", true)]
    [InlineData("RaveOnDb.AspNetUsers.Dump();", true)]
    [InlineData("RaveOnDb.AspNetUsers.Dump();\nRaveOnDb.AspNetRoles.Dump();", false)]
    [InlineData("var ids = RaveOnDb.AspNetUsers.Dump();\nreturn ids;", true)]
    public void ReturnsValueDetection(string query, bool expected)
        => Assert.Equal(expected, LinqCompiler.ReturnsValue(query));

    private static async Task<string> TranslateAsync(string query)
    {
        var compiled = await CompileAsync(query);

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        var queryable = Assert.IsAssignableFrom<IQueryable>(compiled.Run!(context));
        return queryable.ToQueryString();
    }

    private static async Task<LinqCompiledQuery> CompileAsync(string query)
    {
        var compiled = await Compiler.CompileQueryAsync(Model.Value, query);
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Errors));
        return compiled;
    }
}
