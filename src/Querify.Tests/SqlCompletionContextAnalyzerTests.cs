using Querify.Core.Enums.Editors.Completion;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Editors.Completion;
using Querify.Languages.Sql.Completion;
using Xunit;

namespace Querify.Tests;

/// <summary>Le caractere | marque la position du curseur dans chaque requete.</summary>
public class SqlCompletionContextAnalyzerTests
{
    private readonly SqlCompletionContextAnalyzer _analyzer = new();

    [Theory]
    [InlineData("SELECT |", CompletionContextKind.Column)]
    [InlineData("SELECT na|", CompletionContextKind.Column)]
    [InlineData("SELECT COUNT(|", CompletionContextKind.Column)]
    [InlineData("SELECT a, |", CompletionContextKind.Column)]
    [InlineData("SELECT a FROM t WHERE |", CompletionContextKind.Column)]
    [InlineData("SELECT a FROM t WHERE x = 1 AND |", CompletionContextKind.Column)]
    [InlineData("SELECT a FROM t ORDER BY |", CompletionContextKind.Column)]
    [InlineData("SELECT * FROM a JOIN b ON |", CompletionContextKind.Column)]
    [InlineData("UPDATE t SET |", CompletionContextKind.Column)]
    [InlineData("INSERT INTO t (|", CompletionContextKind.Column)]
    [InlineData("INSERT INTO t (a, |", CompletionContextKind.Column)]
    [InlineData("SELECT * FROM t WHERE x = @a |", CompletionContextKind.Column)]
    [InlineData("/* note */ SELECT |", CompletionContextKind.Column)]
    [InlineData("SELECT * FROM |", CompletionContextKind.TableSource)]
    [InlineData("SELECT * FROM a JOIN |", CompletionContextKind.TableSource)]
    [InlineData("SELECT * FROM a, |", CompletionContextKind.TableSource)]
    [InlineData("INSERT INTO |", CompletionContextKind.TableSource)]
    [InlineData("UPDATE |", CompletionContextKind.TableSource)]
    [InlineData("USE |", CompletionContextKind.Database)]
    [InlineData("EXEC |", CompletionContextKind.Procedure)]
    [InlineData("DECLARE @x |", CompletionContextKind.DataType)]
    [InlineData("DECLARE @x INT, @y |", CompletionContextKind.DataType)]
    [InlineData("CREATE PROCEDURE p @a |", CompletionContextKind.DataType)]
    [InlineData("SELECT CAST(a AS |", CompletionContextKind.DataType)]
    [InlineData("SELECT CONVERT(|", CompletionContextKind.DataType)]
    [InlineData("SELECT * FROM Users |", CompletionContextKind.AfterTableName)]
    [InlineData("SELECT * FROM Users W|", CompletionContextKind.AfterTableName)]
    [InlineData("SELECT * FROM dbo.Users |", CompletionContextKind.AfterTableName)]
    [InlineData("SELECT * FROM a JOIN [b] |", CompletionContextKind.AfterTableName)]
    [InlineData("|", CompletionContextKind.General)]
    [InlineData("SEL|", CompletionContextKind.General)]
    [InlineData("SELECT * FROM Users u |", CompletionContextKind.General)]
    [InlineData("SELECT * FROM Users\n|", CompletionContextKind.General)]
    [InlineData("SELECT * FROM t;\n|", CompletionContextKind.General)]
    [InlineData("UPDATE t |", CompletionContextKind.General)]
    [InlineData("CREATE VIEW v AS |", CompletionContextKind.General)]
    [InlineData("SELECT 1\nGO\n|", CompletionContextKind.General)]
    [InlineData("-- SELECT |", CompletionContextKind.None)]
    [InlineData("/* SELECT |", CompletionContextKind.None)]
    [InlineData("SELECT 'abc|", CompletionContextKind.None)]
    [InlineData("SELECT N'it''s |", CompletionContextKind.None)]
    [InlineData("SELECT [ab|", CompletionContextKind.None)]
    [InlineData("SELECT @v|", CompletionContextKind.None)]
    [InlineData("SELECT a AS |", CompletionContextKind.None)]
    [InlineData("SELECT * FROM Users AS |", CompletionContextKind.None)]
    public void Kind(string query, CompletionContextKind expected)
        => Assert.Equal(expected, Analyze(query).Kind);

    [Fact]
    public void Qualifier_AliasAndColumn()
    {
        var context = Analyze("SELECT * FROM dbo.Users u WHERE u.|");

        Assert.Equal(CompletionContextKind.MemberAccess, context.Kind);
        Assert.Equal(new[] { "u" }, context.Qualifier);
        Assert.Equal("Users", context.FindTable("u")?.Name);
        Assert.Equal("dbo", context.FindTable("u")?.Schema);
    }

    [Theory]
    [InlineData("SELECT * FROM dbo.|", CompletionContextKind.TableSource)]
    [InlineData("SELECT * FROM a, Sales.dbo.|", CompletionContextKind.TableSource)]
    [InlineData("EXEC dbo.|", CompletionContextKind.Procedure)]
    [InlineData("SELECT u.|", CompletionContextKind.Column)]
    [InlineData("SELECT * FROM a JOIN b ON a.|", CompletionContextKind.Column)]
    public void Qualifier_ParentKind(string query, CompletionContextKind expected)
    {
        var context = Analyze(query);

        Assert.Equal(CompletionContextKind.MemberAccess, context.Kind);
        Assert.Equal(expected, context.MemberParentKind);
    }

    [Fact]
    public void Qualifier_MultiPart()
    {
        var context = Analyze("SELECT * FROM Sales.[dbo].|");

        Assert.Equal(CompletionContextKind.MemberAccess, context.Kind);
        Assert.Equal(new[] { "Sales", "dbo" }, context.Qualifier);
    }

    [Fact]
    public void Tables_QuotedNamesAliasesAndHints()
    {
        var context = Analyze("SELECT * FROM [My Db].dbo.[Order Lines] ol WITH (NOLOCK) JOIN x.y AS z ON |");

        var lines = context.FindTable("ol");
        Assert.NotNull(lines);
        Assert.Equal("My Db", lines.Database);
        Assert.Equal("dbo", lines.Schema);
        Assert.Equal("Order Lines", lines.Name);

        Assert.Equal("y", context.FindTable("z")?.Name);
        Assert.Equal("z", context.Tables[0].Alias);
    }

    [Fact]
    public void Tables_CommaListAndSubquery()
    {
        var context = Analyze("SELECT * FROM a, b c, (SELECT 1 AS n) d WHERE |");

        Assert.Equal(new[] { "a", "b" }, context.Tables.Select(t => t.Name).Order());
        Assert.Equal("b", context.FindTable("c")?.Name);
    }

    [Fact]
    public void Tables_LimitedToCurrentBatch()
    {
        var context = Analyze("SELECT * FROM a\nGO\nSELECT * FROM b WHERE |\nGO\nSELECT * FROM c");

        Assert.Equal("b", Assert.Single(context.Tables).Name);
    }

    [Fact]
    public void Database_LastUseBeforeCaret()
    {
        var context = Analyze("USE Sales\nGO\nUSE [Stock]\nGO\nSELECT |\nGO\nUSE Other");

        Assert.Equal("Stock", context.Database);
    }

    private CompletionContext Analyze(string query)
    {
        var caret = query.IndexOf('|', StringComparison.Ordinal);
        var text = query.Remove(caret, 1);

        return _analyzer.Analyze(new CompletionRequest
        {
            Language = SyntaxLanguage.Sql,
            Text = text,
            CaretOffset = caret,
            WordStart = CompletionRequest.FindWordStart(text, caret),
            Trigger = CompletionTrigger.Explicit
        });
    }
}
