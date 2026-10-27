using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Workspaces;
using Quill.Code.Analysis;
using Xunit;

namespace Querify.Tests;

/// <summary>Le caractere | marque la position (souris ou curseur) ; Roslyn analyse dans le contexte genere.</summary>
public class LinqAnalysisTests
{
    private static readonly Lazy<LinqModelAssembly> Model = new(() => new LinqCompiler(TestLinq.Services).CompileModel(new EfModelGenerator().Generate(EfModelGeneratorTests.SampleSnapshot())));

    [Fact]
    public async Task Hover_PropertyShowsTypeAndColumn()
    {
        var hover = await HoverAsync("RaveOnDb.AspNetUsers.Where(u => u.Em|ail != null)");

        Assert.NotNull(hover);
        Assert.Contains("string", hover.Signature, StringComparison.Ordinal);
        Assert.Contains("AspNetUsers.Email", hover.Signature, StringComparison.Ordinal);
        Assert.Contains("nvarchar", hover.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hover_LambdaParameterShowsEntity()
    {
        const string query = "RaveOnDb.AspNetUsers.Where(u => u.Email != null)";
        var hover = await TestLinq.HoverAsync(Model.Value, query, query.IndexOf("u =>", StringComparison.Ordinal));

        Assert.NotNull(hover);
        Assert.Contains("AspNetUsers u", hover.Signature, StringComparison.Ordinal);
        Assert.Contains("dbo.AspNetUsers", hover.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hover_LocalVariableShowsInferredType()
    {
        var hover = await HoverAsync("var us|ers = RaveOnDb.AspNetUsers.Where(u => u.EmailConfirmed);\nreturn users;");

        Assert.NotNull(hover);
        Assert.Contains("IQueryable<AspNetUsers> users", hover.Signature, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hover_RangeMapsToQueryText()
    {
        const string query = "RaveOnDb.AspNetUsers";
        var hover = await TestLinq.HoverAsync(Model.Value, query, query.Length - 2);

        Assert.NotNull(hover);
        Assert.Equal(query.IndexOf("AspNetUsers", StringComparison.Ordinal), hover.Start);
        Assert.Equal("AspNetUsers".Length, hover.Length);
    }

    [Fact]
    public async Task Hover_NothingOnPunctuation()
        => Assert.Null(await HoverAsync("RaveOnDb.AspNetUsers.Where(u => u.EmailConfirmed |)"));

    [Fact]
    public async Task Diagnostics_UnknownMemberIsAnError()
    {
        const string query = "RaveOnDb.AspNetUsers.Where(u => u.Mail != null)";
        var diagnostics = await TestLinq.DiagnosticsAsync(Model.Value, query);

        var error = Assert.Single(diagnostics);
        Assert.Equal(CodeDiagnosticSeverity.Error, error.Severity);
        Assert.Equal("Mail", query.Substring(error.Start, error.Length));
    }

    [Fact]
    public async Task Diagnostics_ValidQueryHasNone()
        => Assert.Empty(await TestLinq.DiagnosticsAsync(Model.Value, "RaveOnDb.AspNetUsers.Where(u => u.EmailConfirmed).Select(u => u.Email)"));

    [Fact]
    public async Task Diagnostics_MissingParenthesisAtEndOfQuery()
    {
        const string query = "RaveOnDb.AspNetUsers.Where(u => u.EmailConfirmed";
        var diagnostics = await TestLinq.DiagnosticsAsync(Model.Value, query);

        Assert.Contains(diagnostics, d => d.IsError && d.End <= query.Length);
    }

    [Fact]
    public async Task Diagnostics_WithoutModelOnlySyntax()
    {
        Assert.Empty(await TestLinq.DiagnosticsAsync(null, "RaveOnDb.AspNetUsers.Where(u => u.EmailConfirmed)"));
        Assert.NotEmpty(await TestLinq.DiagnosticsAsync(null, "RaveOnDb.AspNetUsers.Where(u => )"));
    }

    [Fact]
    public async Task Signature_WhereWithActiveParameter()
    {
        var help = await SignatureAsync("RaveOnDb.AspNetUsers.Where(|");

        Assert.NotNull(help);
        Assert.Equal(0, help.ActiveParameter);
        Assert.Contains(help.Signatures, s => s.Prefix.Contains("Where", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Signature_SecondArgument()
    {
        var help = await SignatureAsync("RaveOnDb.AspNetUsers.Select(u => u.Email).Take(10).Skip(|");

        Assert.NotNull(help);
        Assert.Contains(help.Signatures, s => s.Prefix.Contains("Skip", StringComparison.Ordinal));

        var second = await SignatureAsync("RaveOnDb.AspNetUsers.Where(u => u.Email.StartsWith(\"a\", |");
        Assert.NotNull(second);
        Assert.Equal(1, second.ActiveParameter);
        Assert.Contains(second.Signatures, s => s.Prefix.Contains("StartsWith", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Signature_NoneOutsideArguments()
        => Assert.Null(await SignatureAsync("RaveOnDb.AspNetUsers.Count()|"));

    private static Task<HoverInfo?> HoverAsync(string queryWithMarker)
    {
        var offset = queryWithMarker.IndexOf('|', StringComparison.Ordinal);
        return TestLinq.HoverAsync(Model.Value, queryWithMarker.Remove(offset, 1), offset);
    }

    private static Task<SignatureHelpInfo?> SignatureAsync(string queryWithCaret)
    {
        var caret = queryWithCaret.IndexOf('|', StringComparison.Ordinal);
        return TestLinq.SignatureHelpAsync(Model.Value, queryWithCaret.Remove(caret, 1), caret);
    }
}
