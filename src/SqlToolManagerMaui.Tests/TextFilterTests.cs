using SqlToolManagerMaui.Helpers.Tools;
using System.Globalization;
using Xunit;

namespace SqlToolManagerMaui.Tests;

/// <summary>Filtres et tri du DataGrid : les cellules SQL arrivent en texte invariant, la saisie en culture courante.</summary>
public sealed class TextFilterTests : IDisposable
{
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;

    public TextFilterTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previousCulture;
    }

    [Theory]
    [InlineData(null, TextFilterOperator.None, "")]
    [InlineData("   ", TextFilterOperator.None, "")]
    [InlineData("abc", TextFilterOperator.Contains, "abc")]
    [InlineData("!abc", TextFilterOperator.NotContains, "abc")]
    [InlineData("= 10", TextFilterOperator.Equal, "10")]
    [InlineData("<>10", TextFilterOperator.NotEqual, "10")]
    [InlineData("!=10", TextFilterOperator.NotEqual, "10")]
    [InlineData(">=10", TextFilterOperator.GreaterThanOrEqual, "10")]
    [InlineData("<=10", TextFilterOperator.LessThanOrEqual, "10")]
    [InlineData(">10", TextFilterOperator.GreaterThan, "10")]
    [InlineData("<10", TextFilterOperator.LessThan, "10")]
    [InlineData("null", TextFilterOperator.IsNull, "")]
    [InlineData("!NULL", TextFilterOperator.IsNotNull, "")]
    [InlineData(">=", TextFilterOperator.None, "")]
    public void Parse_ReadsOperatorAndOperand(string? text, TextFilterOperator expectedOperator, string expectedOperand)
    {
        var filter = TextFilter.Parse(text);

        Assert.Equal(expectedOperator, filter.Operator);
        Assert.Equal(expectedOperand, filter.Operand);
    }

    [Fact]
    public void Contains_IgnoresCaseAndAccents()
    {
        var filter = TextFilter.Parse("ecole");

        Assert.True(filter.Matches("École Jules Ferry", "École Jules Ferry"));
        Assert.False(filter.Matches("Collège", "Collège"));
        Assert.False(filter.Matches(null, null));
    }

    [Fact]
    public void NotContains_KeepsNullCells()
    {
        var filter = TextFilter.Parse("!test");

        Assert.True(filter.Matches(null, null));
        Assert.True(filter.Matches("prod", "prod"));
        Assert.False(filter.Matches("Test 1", "Test 1"));
    }

    [Fact]
    public void Comparison_IsNumericOnInvariantCells()
    {
        // "9" < "10" en numerique, pas en texte.
        Assert.True(TextFilter.Parse(">9").Matches("10", "10"));
        Assert.False(TextFilter.Parse(">9").Matches("8.5", "8.5"));

        // Saisie francaise "1,5" contre une cellule invariante "1.5".
        Assert.True(TextFilter.Parse("=1,5").Matches("1.5", "1.5"));
        Assert.True(TextFilter.Parse("<=2").Matches(2, "2"));
    }

    [Fact]
    public void Comparison_IsChronologicalOnDates()
    {
        var filter = TextFilter.Parse(">=01/10/2024");

        Assert.True(filter.Matches("2024-10-15 08:00:00", "2024-10-15 08:00:00"));
        Assert.False(filter.Matches("2024-09-30", "2024-09-30"));
        Assert.True(filter.Matches(new DateTime(2025, 1, 1), "01/01/2025"));
    }

    [Fact]
    public void Comparison_OnDifferentKinds_NeverMatchesOrder()
    {
        Assert.False(TextFilter.Parse(">10").Matches("abc", "abc"));
        Assert.True(TextFilter.Parse("!=10").Matches("abc", "abc"));
        Assert.False(TextFilter.Parse(">10").Matches(null, null));
    }

    [Fact]
    public void Equal_OnText_IgnoresCase()
    {
        Assert.True(TextFilter.Parse("=paris").Matches("Paris", "Paris"));
        Assert.False(TextFilter.Parse("=paris").Matches("Paris 15", "Paris 15"));
    }

    [Fact]
    public void NullKeywords_TestPresence()
    {
        Assert.True(TextFilter.Parse("NULL").Matches(null, null));
        Assert.False(TextFilter.Parse("NULL").Matches("x", "x"));
        Assert.True(TextFilter.Parse("!NULL").Matches("x", "x"));
        Assert.False(TextFilter.Parse("!NULL").Matches(DBNull.Value, null));
    }

    [Fact]
    public void Sort_OrdersNullsThenNumbersThenDatesThenText()
    {
        object?[] values = ["b", "10", null, "2024-01-01", "9", "A"];
        object?[] expected = [null, 9m, 10m, new DateTime(2024, 1, 1), "A", "b"];

        var sorted = values
            .Select(ValueComparison.Normalize)
            .Order(Comparer<object?>.Create(ValueComparison.Compare))
            .ToList();

        Assert.Equal(expected, sorted);
    }
}
