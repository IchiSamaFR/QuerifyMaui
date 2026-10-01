using SqlToolManagerMaui.Models.Queries;

namespace SqlToolManagerMaui.Views.Controls.Queries;

/// <summary>
/// Grille en lecture seule d'un jeu de resultats : en-tete fixe, lignes virtualisees,
/// defilement horizontal quand les colonnes depassent la largeur disponible.
/// Construite en code car le nombre de colonnes n'est connu qu'a l'execution.
/// </summary>
public sealed class QueryResultGrid : ContentView
{
    private const double RowNumberWidth = 56;
    private const double MinColumnWidth = 60;
    private const double MaxColumnWidth = 360;
    private const double ApproximateCharWidth = 7.5;
    private const double CellHorizontalPadding = 24;

    /// <summary>Nombre de lignes echantillonnees pour dimensionner les colonnes.</summary>
    private const int WidthSampleRowCount = 200;

    public static readonly BindableProperty ResultSetProperty = BindableProperty.Create(
        nameof(ResultSet), typeof(QueryResultSet), typeof(QueryResultGrid), null,
        propertyChanged: (bindable, _, _) => ((QueryResultGrid)bindable).Rebuild());

    /// <summary>Jeu de resultats affiche.</summary>
    public QueryResultSet? ResultSet
    {
        get => (QueryResultSet?)GetValue(ResultSetProperty);
        set => SetValue(ResultSetProperty, value);
    }

    private void Rebuild()
    {
        var resultSet = ResultSet;
        if (resultSet is null)
        {
            Content = null;
            return;
        }

        var widths = ComputeColumnWidths(resultSet);

        var rows = new CollectionView
        {
            ItemsSource = resultSet.Rows,
            ItemTemplate = new DataTemplate(() => new QueryResultRowView(RowNumberWidth, widths)),
            SelectionMode = SelectionMode.None,
            ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
            WidthRequest = RowNumberWidth + widths.Sum() + widths.Count,
            HorizontalOptions = LayoutOptions.Start,
        };

        var layout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
            },
            HorizontalOptions = LayoutOptions.Start,
        };
        layout.Add(CreateHeader(resultSet, widths), 0, 0);
        layout.Add(rows, 0, 1);

        Content = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = layout,
            HorizontalOptions = LayoutOptions.Start,
        };
    }

    private static Grid CreateHeader(QueryResultSet resultSet, IReadOnlyList<double> widths)
    {
        var header = new Grid();
        header.SetDynamicResource(StyleProperty, "QueryResultRowStyle");

        header.ColumnDefinitions.Add(new ColumnDefinition(RowNumberWidth));
        var corner = new Label();
        corner.SetDynamicResource(Label.StyleProperty, "QueryResultHeaderStyle");
        header.Add(corner, 0);

        for (var i = 0; i < resultSet.Columns.Count; i++)
        {
            var column = resultSet.Columns[i];
            header.ColumnDefinitions.Add(new ColumnDefinition(widths[i]));

            var label = new Label { Text = column.Name };
            label.SetDynamicResource(Label.StyleProperty, "QueryResultHeaderStyle");
            ToolTipProperties.SetText(label, $"{column.Name} ({column.TypeName})");
            header.Add(label, i + 1);
        }

        return header;
    }

    /// <summary>Largeur estimee d'apres le plus long texte (en-tete ou echantillon de lignes), bornee.</summary>
    private static List<double> ComputeColumnWidths(QueryResultSet resultSet)
    {
        var sample = resultSet.Rows.Take(WidthSampleRowCount).ToList();
        var widths = new List<double>(resultSet.Columns.Count);

        for (var i = 0; i < resultSet.Columns.Count; i++)
        {
            var longest = resultSet.Columns[i].Name.Length;
            foreach (var row in sample)
            {
                var length = i < row.Cells.Length ? row.Cells[i]?.Length ?? 4 : 0;
                if (length > longest)
                    longest = length;
            }

            widths.Add(Math.Clamp((longest * ApproximateCharWidth) + CellHorizontalPadding, MinColumnWidth, MaxColumnWidth));
        }

        return widths;
    }
}
