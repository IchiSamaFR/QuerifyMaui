using Querify.Databases.Enums;
using Querify.Languages.Sql.Editors;
using Xunit;

namespace Querify.Tests;

/// <summary>Chaque moteur de connexion ecrit son SQL dans son propre dialecte.</summary>
public sealed class SqlDialectsTests
{
    [Fact]
    public void EveryProvider_HasItsOwnDialect()
    {
        var dialects = Enum.GetValues<DatabaseProvider>().Select(provider => SqlDialects.For(provider)).ToArray();

        Assert.Equal(dialects.Length, dialects.Distinct().Count());
    }

    [Fact]
    public void WithoutConnection_DialectIsSqlServer()
        => Assert.Same(SqlDialects.For(DatabaseProvider.SqlServer), SqlDialects.For(null));

    [Theory]
    [InlineData(DatabaseProvider.SqlServer, "SELECT TOP (100) *\nFROM [Shop].[dbo].[Users];")]
    [InlineData(DatabaseProvider.MySql, "SELECT *\nFROM `dbo`.`Users`\nLIMIT 100;")]
    [InlineData(DatabaseProvider.PostgreSql, "SELECT *\nFROM \"dbo\".\"Users\"\nLIMIT 100;")]
    [InlineData(DatabaseProvider.Oracle, "SELECT *\nFROM \"dbo\".\"Users\"\nFETCH FIRST 100 ROWS ONLY;")]
    [InlineData(DatabaseProvider.Sqlite, "SELECT *\nFROM \"dbo\".\"Users\"\nLIMIT 100;")]
    public void SelectFirstRows_FollowsTheEngine(DatabaseProvider provider, string expected)
    {
        var dialect = SqlDialects.For(provider);

        Assert.Equal(expected, dialect.SelectFirstRows(dialect.FormatObjectName("Shop", "dbo", "Users", alwaysQuote: true), 100));
    }

    [Fact]
    public void Parameters_UseThePrefixOfTheEngine()
    {
        Assert.Equal("@UserId", SqlDialects.For(DatabaseProvider.SqlServer).FormatParameter("UserId"));
        Assert.Equal(":UserId", SqlDialects.For(DatabaseProvider.Oracle).FormatParameter("UserId"));
    }
}
