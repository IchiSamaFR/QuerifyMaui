using Microsoft.EntityFrameworkCore;
using SqlToolManagerMaui.Languages.Linq;
using Xunit;

namespace SqlToolManagerMaui.Tests;

/// <summary>Requetes compilees contre le modele genere, traduites en SQL sans connexion.</summary>
public class LinqCompilerTests
{
    private const string ConnectionString = "Server=localhost;Database=RaveOnDb;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly LinqCompiler Compiler = new();

    private static readonly Lazy<LinqModelAssembly> Model = new(() => Compiler.CompileModel(new EfModelGenerator().Generate(EfModelGeneratorTests.SampleSnapshot())));

    [Fact]
    public void MethodSyntax_TranslatesToSql()
    {
        var sql = Translate("""
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
    public void QuerySyntax_TranslatesToSql()
    {
        var sql = Translate("from u in RaveOnDb.AspNetUsers where u.AccessFailedCount > 3 select u.Id");

        Assert.Contains("[AccessFailedCount] > 3", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Join_OnCompositeKeyTable()
    {
        var sql = Translate("""
            from u in RaveOnDb.AspNetUsers
            join r in RaveOnDb.AspNetUserRoles on u.Id equals r.UserId
            select new { u.Email, r.RoleId }
            """);

        Assert.Contains("INNER JOIN", sql, StringComparison.Ordinal);
        Assert.Contains("[AspNetUserRoles]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Scalar_ReturnsValueType()
    {
        var compiled = Compile("40 + 2");

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        Assert.Equal(42, compiled.Run!(context));
    }

    [Fact]
    public void Parameters_DeclaredAsTypedVariables()
    {
        // Nom seul ou @Nom (identifiant verbatim) : meme variable, comme un parametre SQL.
        using var compiled = Compiler.CompileQuery(Model.Value, "UserId + @UserId", [new LinqParameter("UserId", typeof(int?), 21)]);
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        Assert.Equal(42, compiled.Run!(context));
    }

    [Fact]
    public void Parameters_SentAsSqlParameters()
    {
        using var compiled = Compiler.CompileQuery(Model.Value, "RaveOnDb.AspNetUsers.Where(u => u.Email == @Email)",
            [new LinqParameter("Email", typeof(string), "a@b.fr")]);
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        var sql = Assert.IsAssignableFrom<IQueryable>(compiled.Run!(context)).ToQueryString();

        // ToQueryString declare la valeur en tete, mais le WHERE compare bien a un parametre.
        Assert.Contains("= @", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Statements_ReturnVariable()
    {
        var sql = Translate("""
            var ids = RaveOnDb.AspNetUsers.Select(u => u.Id);
            return ids;
            """);

        Assert.Contains("[Id]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Statements_ComposeBeforeReturn()
    {
        var sql = Translate("""
            var counts = RaveOnDb.AspNetUsers.Select(u => u.AccessFailedCount);
            return counts.Where(i => i > 10);
            """);

        Assert.Contains("> 10", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Statements_WithoutReturnGiveNull()
    {
        var compiled = Compile("var ids = RaveOnDb.AspNetUsers.Select(u => u.Id);");

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        Assert.Null(compiled.Run!(context));
    }

    [Fact]
    public void Statements_ErrorsPointToQueryText()
    {
        var compiled = Compiler.CompileQuery(Model.Value, "var users = RaveOnDb.AspNetUsers;\nreturn users.Wher(u => true);");

        Assert.False(compiled.Success);
        Assert.Equal(2, compiled.Errors[0].Line);
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
    public void Errors_PointToQueryText()
    {
        var compiled = Compiler.CompileQuery(Model.Value, "RaveOnDb.AspNetUsers\n    .Wher(u => true)");

        Assert.False(compiled.Success);
        var error = compiled.Errors[0];
        Assert.Equal("CS1061", error.Id);
        Assert.Equal(2, error.Line);
    }

    [Fact]
    public void Errors_UnknownContext()
    {
        var compiled = Compiler.CompileQuery(Model.Value, "Northwind.Orders");

        Assert.Contains(compiled.Errors, e => e.Id == "CS0103" && e.Line == 1 && e.Column == 1);
    }

    [Fact]
    public void Dump_CollectsEachValueAndContinues()
    {
        var compiled = Compile("""
            RaveOnDb.AspNetUsers.Dump("Utilisateurs");
            var total = 40.Dump();
            return total + 2;
            """);

        List<(object? Value, string? Title)> dumps = [];
        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        var result = compiled.Execute!(context, (value, title) => dumps.Add((value, title)));

        Assert.Equal(42, result);
        Assert.Equal(2, dumps.Count);
        Assert.IsAssignableFrom<IQueryable>(dumps[0].Value);
        Assert.Equal("Utilisateurs", dumps[0].Title);
        Assert.Equal(40, dumps[1].Value);
        Assert.Null(dumps[1].Title);
    }

    [Theory]
    [InlineData("RaveOnDb.AspNetUsers", true)]
    [InlineData("RaveOnDb.AspNetUsers.Dump();", true)]
    [InlineData("RaveOnDb.AspNetUsers.Dump();\nRaveOnDb.AspNetRoles.Dump();", false)]
    [InlineData("var ids = RaveOnDb.AspNetUsers.Dump();\nreturn ids;", true)]
    public void ReturnsValueDetection(string query, bool expected)
        => Assert.Equal(expected, LinqCompiler.ReturnsValue(query));

    private static string Translate(string query)
    {
        var compiled = Compile(query);

        using var context = Compiler.CreateContext(Model.Value, ConnectionString);
        var queryable = Assert.IsAssignableFrom<IQueryable>(compiled.Run!(context));
        return queryable.ToQueryString();
    }

    private static LinqCompiledQuery Compile(string query)
    {
        var compiled = Compiler.CompileQuery(Model.Value, query);
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Errors));
        return compiled;
    }
}
