using SqlToolManagerMaui.Helpers.Tools;
using System.Data.Common;
using Xunit;

namespace SqlToolManagerMaui.Tests;

/// <summary>Le masque ne doit jamais alterer le mot de passe reinjecte, quelle que soit sa forme.</summary>
public class ConnectionStringPasswordHelperTests
{
    [Theory]
    [InlineData("Server=s;User Id=u;Password=secret;")]
    [InlineData("Server=s;Pwd=secret")]
    [InlineData("Server=s;Password=\"a;b\";")]
    [InlineData("Server=s;Password=\"a'b\"\"c;\";")]
    public void Unmask_MaskUnchanged_RestoresStoredString(string stored)
    {
        var masked = ConnectionStringPasswordHelper.Mask(stored);

        Assert.Contains(ConnectionStringPasswordHelper.MaskValue, masked, StringComparison.Ordinal);
        Assert.Equal(stored, ConnectionStringPasswordHelper.Unmask(masked, stored));
    }

    [Theory]
    [InlineData("Server=s;Password=\"a\"\"b;c\";", "a\"b;c")]
    [InlineData("Server=s;Password='it''s';", "it's")]
    [InlineData("Server=s;Password=\"a'b\"\"c;\";", "a'b\"c;")]
    public void Unmask_QuotedPassword_KeepsAdoNetValue(string stored, string expectedPassword)
    {
        var restored = ConnectionStringPasswordHelper.Unmask(ConnectionStringPasswordHelper.Mask(stored), stored);

        Assert.Equal(expectedPassword, PasswordOf(restored));
    }

    [Fact]
    public void Unmask_RetypedPassword_Wins()
    {
        const string Stored = "Server=s;Password=old;";

        Assert.Equal("Server=s;Password=new;", ConnectionStringPasswordHelper.Unmask("Server=s;Password=new;", Stored));
    }

    [Fact]
    public void Mask_WithoutPassword_LeavesStringUntouched()
    {
        const string ConnectionString = "Server=s;Integrated Security=true";

        Assert.False(ConnectionStringPasswordHelper.HasPassword(ConnectionString));
        Assert.Equal(ConnectionString, ConnectionStringPasswordHelper.Mask(ConnectionString));
    }

    private static string? PasswordOf(string connectionString)
        => new DbConnectionStringBuilder { ConnectionString = connectionString }["password"] as string;
}
