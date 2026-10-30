using Querify.Core.Enums.Dashboards;
using Querify.Core.Models.Dashboards;
using Querify.Core.Models.Editors;
using Querify.Core.Serialization;
using Xunit;
using Quill.Code.Execution;

namespace Querify.Tests;

public class DashboardTests
{
    private const string SHOWS_DASHBOARD = """
        {
          "connection": "Recette",
          "parameters": [ { "name": "UserId", "type": "Integer", "default": "1" } ],
          "pages": [
            {
              "id": "accueil", "title": "Mes shows",
              "cards": [
                { "title": "Shows", "column": 0, "row": 0, "columnSpan": 8, "rowSpan": 4, "view": "grid",
                  "query": "SELECT Id, Name FROM Shows WHERE OwnerId = @UserId",
                  "navigation": { "page": "show", "parameters": [ { "parameter": "ShowId", "column": "Id" } ] } },
                { "column": 8, "row": 6, "columnSpan": 4, "rowSpan": 2, "view": "barChart" }
              ]
            },
            { "id": "show", "title": "Détail" }
          ]
        }
        """;

    [Fact]
    public void ReadsDashboard()
    {
        var definition = DashboardSerializer.Deserialize(SHOWS_DASHBOARD);

        Assert.Equal(12, definition.Columns);
        Assert.Equal(6, definition.Rows);
        var parameter = Assert.Single(definition.Parameters);
        Assert.Equal(DashboardParameterType.Integer, parameter.Type);

        var cards = definition.Pages[0].Cards;
        Assert.Equal(DashboardCardKind.Grid, cards[0].View);
        Assert.Equal(DashboardCardKind.BarChart, cards[1].View);
        Assert.Equal("show", cards[0].Navigation!.Page);
        Assert.False(string.IsNullOrEmpty(cards[1].Id));
    }

    [Fact]
    public void SerializationRoundTrip()
    {
        var definition = DashboardSerializer.Deserialize(SHOWS_DASHBOARD);

        var json = DashboardSerializer.Serialize(definition);
        var read = DashboardSerializer.Deserialize(json);

        Assert.Contains("\"view\": \"BarChart\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
        Assert.Equal(definition.Pages[0].Cards[0].Query, read.Pages[0].Cards[0].Query);
        Assert.Equal("Id", read.Pages[0].Cards[0].Navigation!.Parameters[0].Column);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ \"pages\": [] }")]
    [InlineData("{ \"pages\": [ { \"id\": \"a\" }, { \"id\": \"A\" } ] }")]
    [InlineData("{ \"pages\": [ { \"title\": \"sans id\" } ] }")]
    [InlineData("{ \"version\": 99, \"pages\": [ { \"id\": \"a\" } ] }")]
    public void RejectsInvalidFiles(string json)
        => Assert.Throws<FormatException>(() => DashboardSerializer.Deserialize(json));

    [Theory]
    [InlineData(DashboardParameterType.Integer, "42", 42L)]
    [InlineData(DashboardParameterType.Boolean, "oui", true)]
    [InlineData(DashboardParameterType.Text, "abc", "abc")]
    [InlineData(DashboardParameterType.Integer, "", null)]
    public void ConvertsParameterValues(DashboardParameterType type, string input, object? expected)
    {
        var parameter = new DashboardParameterDefinition { Name = "P", Type = type };

        Assert.True(parameter.TryConvert(input, out var value, out var error));
        Assert.Null(error);
        Assert.Equal(expected, value);
    }

    [Fact]
    public void RejectsInvalidParameterValue()
    {
        var parameter = new DashboardParameterDefinition { Name = "P", Type = DashboardParameterType.Integer };

        Assert.False(parameter.TryConvert("abc", out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void NavigationTakesValuesFromRowAndParameters()
    {
        var navigation = new DashboardNavigationDefinition
        {
            Page = "show",
            Parameters = DashboardNavigationDefinition.ParseParameters("ShowId = Id\nUser = @UserId; Mode = 'detail'"),
        };
        var resultSet = new QueryResultSet(1, [new QueryResultColumn("Id", "int"), new QueryResultColumn("Name", "nvarchar")], [], isTruncated: false);
        var row = new QueryResultRow(1, ["7", "Rave"]);

        var values = navigation.Resolve(new Dictionary<string, string?> { ["UserId"] = "3" }, resultSet, row);

        Assert.Equal("7", values["ShowId"]);
        Assert.Equal("3", values["User"]);
        Assert.Equal("detail", values["Mode"]);
        Assert.Equal("ShowId = Id\nUser = @UserId\nMode = 'detail'", navigation.FormatParameters());
    }

    [Fact]
    public void GridGrowsWithCardsAndDesigner()
    {
        var session = new DashboardSession(new EditorDocument("Dashboard", filePath: null, fileText: string.Empty));
        var definition = DashboardSerializer.Deserialize(SHOWS_DASHBOARD);
        session.Load(definition, SHOWS_DASHBOARD, error: null);
        session.Open(definition.Pages[0], asRoot: true);

        // La seconde carte descend jusqu'a la ligne 8.
        Assert.Equal(8, session.RowCount);

        session.Mode = DashboardMode.Design;
        Assert.Equal(9, session.RowCount);
    }

    [Fact]
    public void DropPreviewFollowsHoveredCell()
    {
        var session = new DashboardSession(new EditorDocument("Dashboard", filePath: null, fileText: string.Empty));
        var definition = DashboardSerializer.Deserialize(SHOWS_DASHBOARD);
        session.Load(definition, SHOWS_DASHBOARD, error: null);
        var page = session.Open(definition.Pages[0], asRoot: true);
        session.Mode = DashboardMode.Design;

        session.DropCell = new DashboardCell(10, 3);
        Assert.Null(session.DropPreview);

        // Carte de 8 colonnes : reduite au bord droit de la grille de 12.
        session.DraggedCard = page.Cards[0];
        Assert.Equal(new DashboardCardPlacement(4, 3, 8, 4), session.DropPreview);

        session.DraggedCard = null;
        Assert.False(session.HasDropPreview);
    }

    [Fact]
    public void DesignerChangesWriteDefinition()
    {
        var session = new DashboardSession(new EditorDocument("Dashboard", filePath: null, fileText: string.Empty));
        var definition = DashboardSerializer.Deserialize(SHOWS_DASHBOARD);
        session.Load(definition, SHOWS_DASHBOARD, error: null);
        var page = session.Open(definition.Pages[0], asRoot: true);
        var changes = 0;
        session.DefinitionChanged += (_, _) => changes++;

        page.Cards[0].ColumnSpan = 6;
        page.Cards[0].ValueColumnsText = "Total, Moyenne";
        session.AddCard(new DashboardCardDefinition { Column = 0, Row = 8 });

        Assert.Equal(6, definition.Pages[0].Cards[0].ColumnSpan);
        Assert.Equal(new[] { "Total", "Moyenne" }, definition.Pages[0].Cards[0].ValueColumns);
        Assert.Equal(3, definition.Pages[0].Cards.Count);
        Assert.Equal(3, changes);
    }

    [Fact]
    public void BackRestoresParameterValues()
    {
        var session = new DashboardSession(new EditorDocument("Dashboard", filePath: null, fileText: string.Empty));
        var definition = DashboardSerializer.Deserialize(SHOWS_DASHBOARD);
        session.Load(definition, SHOWS_DASHBOARD, error: null);
        var home = session.Open(definition.Pages[0], asRoot: true);

        session.ApplyValues(new Dictionary<string, string?> { ["UserId"] = "9" });
        session.Open(definition.Pages[1], asRoot: false);
        session.Back();

        Assert.Same(home, session.Current);
        Assert.Equal("1", session.Parameters[0].Value);
    }

    [Fact]
    public void CommittedParameterValuesAreWrittenAsDefaults()
    {
        var session = new DashboardSession(new EditorDocument("Dashboard", filePath: null, fileText: string.Empty));
        var definition = DashboardSerializer.Deserialize(SHOWS_DASHBOARD);
        session.Load(definition, SHOWS_DASHBOARD, error: null);
        var changes = 0;
        session.DefinitionChanged += (_, _) => changes++;

        session.CommitParameterValues();
        Assert.Equal(0, changes);

        session.Parameters[0].Value = "abc";
        session.CommitParameterValues();
        Assert.Equal("1", definition.Parameters[0].Default);
        Assert.Equal(0, changes);

        session.Parameters[0].Value = "42";
        session.CommitParameterValues();
        Assert.Equal("42", definition.Parameters[0].Default);
        Assert.Equal(1, changes);
        Assert.Contains("\"default\": \"42\"", DashboardSerializer.Serialize(definition));
    }

    [Fact]
    public void ButtonRaisesNavigation()
    {
        var session = new DashboardSession(new EditorDocument("Dashboard", filePath: null, fileText: string.Empty));
        var definition = DashboardSerializer.Deserialize(SHOWS_DASHBOARD);
        session.Load(definition, SHOWS_DASHBOARD, error: null);
        var page = session.Open(definition.Pages[0], asRoot: true);
        DashboardNavigationRequest? request = null;
        session.NavigationRequested += (_, e) => request = e;

        page.Cards[0].ActivateRowCommand.Execute(new QueryResultRow(1, ["7", "Rave"]));

        Assert.NotNull(request);
        Assert.Same(page.Cards[0], request.Card);
    }

    [Fact]
    public void MissingCollectionsDeserializeEmpty()
    {
        var definition = DashboardSerializer.Deserialize("""{ "pages": [ { "id": "vide" }, { "id": "carte", "cards": [ { "view": "grid" } ] } ] }""");

        Assert.Empty(definition.Parameters);
        Assert.Empty(definition.Pages[0].Cards);
        Assert.Empty(definition.Pages[1].Cards[0].ValueColumns);
    }
}
