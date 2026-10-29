using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.Analysis;
using Quill.Code.CSharp.Compilation;
using Quill.Code.Scripting;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Querify.Tests;

/// <summary>Requetes compilees contre le modele genere, traduites en SQL sans connexion.</summary>
public class LinqCompilerTests
{
    private const string ConnectionString = "Server=localhost;Database=RaveOnDb;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly Lazy<LinqGeneratedModelAssembly> Model = new(() => LinqCompiler.CompileModelAsync(new EfModelGenerator().Generate(EfModelGeneratorTests.SampleSnapshot())).GetAwaiter().GetResult());

    [Fact]
    public async Task MethodSyntax_TranslatesToSql()
    {
        var sql = await TranslateAsync("""
            Db.AspNetUsers
                .Where(u => u.Email != null && !u.EmailConfirmed)
                .OrderBy(u => u.Email)
                .Select(u => new { u.Id, u.Email })
                .Dump();
            """);

        Assert.Contains("WHERE", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY", sql, StringComparison.Ordinal);
        Assert.Contains("[Email]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QuerySyntax_TranslatesToSql()
    {
        var sql = await TranslateAsync("(from u in Db.AspNetUsers where u.AccessFailedCount > 3 select u.Id).Dump();");

        Assert.Contains("[AccessFailedCount] > 3", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Join_OnCompositeKeyTable()
    {
        var sql = await TranslateAsync("""
            (from u in Db.AspNetUsers
             join r in Db.AspNetUserRoles on u.Id equals r.UserId
             select new { u.Email, r.RoleId }).Dump();
            """);

        Assert.Contains("INNER JOIN", sql, StringComparison.Ordinal);
        Assert.Contains("[AspNetUserRoles]", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scalar_IsDumped()
    {
        var compiled = await CompileAsync("(40 + 2).Dump();");

        Assert.Equal([42], await DumpsAsync(compiled));
    }

    [Fact]
    public async Task Statements_ComposeBeforeDump()
    {
        var sql = await TranslateAsync("""
            var counts = Db.AspNetUsers.Select(u => u.AccessFailedCount);
            counts.Where(i => i > 10).Dump();
            """);

        Assert.Contains("> 10", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Usings_DeclaredByTheQuery()
    {
        var compiled = await CompileAsync("""
            using System.Text;

            new StringBuilder("a").Append('b').ToString().Dump();
            """);

        Assert.Equal(["ab"], await DumpsAsync(compiled));
    }

    [Fact]
    public async Task Types_DeclaredAfterTheStatements()
    {
        var compiled = await CompileAsync("""
            new Greeter().Say().Dump();

            class Greeter
            {
                public string Say() => "hi";
            }
            """);

        Assert.Equal(["hi"], await DumpsAsync(compiled));
    }

    [Fact]
    public async Task Context_IsDbOfTheGeneratedType()
    {
        var compiled = await CompileAsync("Db.GetType().Name.Dump();");

        Assert.Equal(["RaveOnDbContext"], await DumpsAsync(compiled));
    }

    [Fact]
    public async Task ReturnedValue_IsIgnored()
    {
        var compiled = await CompileAsync("return 42;");

        Assert.Empty(await DumpsAsync(compiled));
    }

    [Fact]
    public async Task Async_IsAwaited()
    {
        var compiled = await CompileAsync("""
            await Task.Yield();
            42.Dump();
            """);

        Assert.Equal([42], await DumpsAsync(compiled));
    }

    [Theory]
    [InlineData("")]
    [InlineData("using System;")]
    [InlineData("class Greeter { }")]
    public async Task WithoutStatements_IsAnError(string query)
    {
        var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, query);

        Assert.False(compiled.Success);
        Assert.Equal("CS5001", Assert.Single(compiled.Errors).Code);
    }

    [Fact]
    public async Task Parameters_ExposedByNameAndType()
    {
        // Nom sans @, type du dashboard (int?) : l'arithmetique reste celle du type.
        using var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, "(UserId * 2).Dump();", [new LinqParameter("@UserId", typeof(int?), 16)]);
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

        Assert.Equal([32], await DumpsAsync(compiled));
    }

    [Fact]
    public async Task Parameters_SentAsSqlParameters()
    {
        using var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, "Db.AspNetUsers.Where(u => u.Email == Email).Dump();", [new LinqParameter("Email", typeof(string), "a@b.fr")]);
        Assert.True(compiled.Success, string.Join("\n", compiled.Errors));

        var sql = Assert.IsAssignableFrom<IQueryable>(Assert.Single(await DumpsAsync(compiled))).ToQueryString();

        // ToQueryString declare la valeur en tete, mais le WHERE compare bien a un parametre.
        Assert.Contains("= @", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Parameters_UnconvertibleValueIsAnError()
    {
        var ex = await Assert.ThrowsAsync<LinqCompilationException>(() => LinqCompiler.CompileQueryAsync(Model.Value, "Count.Dump();", [new LinqParameter("@Count", typeof(int?), "abc")]));

        Assert.Contains("Count", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Errors_PointToQueryText()
    {
        const string query = "var users = Db.AspNetUsers;\nusers.Wher(u => true);";
        var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, query);

        Assert.False(compiled.Success);
        var error = compiled.Errors[0];
        Assert.Equal("CS1061", error.Code);
        Assert.Equal("Wher", query.Substring(error.Start, error.Length));
    }

    [Fact]
    public async Task Errors_UnknownContext()
    {
        const string query = "Northwind.Orders.Dump();";
        var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, query);

        Assert.Contains(compiled.Errors, e => e.Code == "CS0103" && e.Start == query.IndexOf("Northwind", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dump_CollectsEachValueAndContinues()
    {
        var compiled = await CompileAsync("""
            Db.AspNetUsers.Dump("Utilisateurs");
            var total = 40.Dump();
            (total + 2).Dump();
            """);

        List<(object? Value, string? Title)> dumps = [];
        using var context = LinqCompiler.CreateContext(Model.Value, ConnectionString);
        await LinqCompiler.RunAsync(Model.Value, compiled, context, (value, title) => dumps.Add((value, title)));

        Assert.Equal(3, dumps.Count);
        Assert.IsAssignableFrom<IQueryable>(dumps[0].Value);
        Assert.Equal("Utilisateurs", dumps[0].Title);
        Assert.Equal(40, dumps[1].Value);
        Assert.Null(dumps[1].Title);
        Assert.Equal(42, dumps[2].Value);
    }

    [Fact]
    public async Task Log_IsRedirectedWithItsLevel()
    {
        List<(LogLevel Level, string Text)> output = [];
        var compiled = await CompileAsync("""
            Log.Information("a");
            Log.Warning($"{1}+{2}");
            Log.Write(LogLevel.Error, "{b}");
            Log.Write(LogLevel.None, "ignore");
            """, (level, text) => output.Add((level, text)));

        await DumpsAsync(compiled);

        Assert.Equal(new[] { (LogLevel.Information, "a"), (LogLevel.Warning, "1+2"), (LogLevel.Error, "{b}") }, output);
    }

    private static async Task<string> TranslateAsync(string query)
    {
        var compiled = await CompileAsync(query);
        return Assert.IsAssignableFrom<IQueryable>(Assert.Single(await DumpsAsync(compiled))).ToQueryString();
    }

    /// <summary>
    /// Execute sans ouvrir de connexion et rend les valeurs affichees : une requete IQueryable n'est que traduite
    /// (ToQueryString), apres coup, d'ou un contexte non libere ici.
    /// </summary>
    private static async Task<List<object?>> DumpsAsync(CSharpCompilationResult compiled)
    {
        List<object?> values = [];
        await LinqCompiler.RunAsync(Model.Value, compiled, LinqCompiler.CreateContext(Model.Value, ConnectionString), (value, _) => values.Add(value));
        return values;
    }

    private static async Task<CSharpCompilationResult> CompileAsync(string query, Action<LogLevel, string>? log = null)
    {
        var compiled = await LinqCompiler.CompileQueryAsync(Model.Value, query, log: log);
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Errors));
        return compiled;
    }
}
