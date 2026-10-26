using Querify.Languages.Sql.Analysis;
using Xunit;

namespace Querify.Tests;

public class SqlDiagnosticProviderTests
{
    [Fact]
    public void ValidScript_HasNoError()
        => Assert.Empty(SqlDiagnosticProvider.Parse("SELECT u.Id, u.Email FROM dbo.Users u WHERE u.Id = @UserId\nGO\nSELECT 1"));

    [Fact]
    public void Typo_UnderlinesTheToken()
    {
        const string text = "SELECT * FORM dbo.Users";
        var error = Assert.Single(SqlDiagnosticProvider.Parse(text));

        Assert.True(error.IsError);
        Assert.InRange(error.Start, 0, text.Length - 1);
        Assert.True(error.Length > 0);
    }

    [Fact]
    public void UnfinishedStatement_PointsAtTheEnd()
    {
        const string text = "SELECT * FROM dbo.Users WHERE   ";
        var error = Assert.Single(SqlDiagnosticProvider.Parse(text));

        Assert.True(error.End <= text.TrimEnd().Length);
    }
}
