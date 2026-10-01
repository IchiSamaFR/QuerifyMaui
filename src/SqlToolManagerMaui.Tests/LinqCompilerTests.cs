using Microsoft.EntityFrameworkCore;
using SqlToolManagerMaui.Services.Linq;
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
