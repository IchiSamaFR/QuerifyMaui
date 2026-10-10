using CommunityToolkit.Maui;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Models.Queries;
using System.Globalization;

namespace SqlToolManagerMaui.Views.Controls.Dashboards;

/// <summary>
/// Graphique d'une carte (LiveCharts2). Les series dependent des colonnes du resultat, connues
/// seulement a l'execution : elles sont recalculees en code a chaque changement.
/// </summary>
public sealed partial class DashboardChartView : ContentView
{
    // Gris lisible sur les deux themes : les axes ne suivent pas les ressources MAUI.
    private static readonly SKColor LabelColor = new(0x9A, 0x9A, 0x9A);

    [BindableProperty(PropertyChangedMethodName = nameof(OnChartChanged))]
    public partial QueryResultSet? ResultSet { get; set; }

    [BindableProperty(PropertyChangedMethodName = nameof(OnChartChanged))]
    public partial DashboardCardKind View { get; set; } = DashboardCardKind.BarChart;

    /// <summary>Colonne des libelles ; vide = la premiere.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnChartChanged))]
    public partial string? LabelColumn { get; set; }

    /// <summary>Colonnes de valeurs separees par des virgules ; vide = toutes les autres colonnes numeriques.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnChartChanged))]
    public partial string? ValueColumns { get; set; }

    public DashboardChartView()
    {
        InitializeComponent();

        // Un pinceau par graphique : LiveCharts ne partage pas un paint entre deux canevas.
        Pie.LegendTextPaint = new SolidColorPaint(LabelColor);
        Cartesian.LegendTextPaint = new SolidColorPaint(LabelColor);
    }

    private static void OnChartChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DashboardChartView)bindable).Show(((DashboardChartView)bindable).Render());

    private void Show(VisualElement? visible)
    {
        NoValueLabel.IsVisible = visible == NoValueLabel;
        Pie.IsVisible = visible == Pie;
        Cartesian.IsVisible = visible == Cartesian;
    }

    /// <summary>Alimente le rendu adapte et le renvoie (null : rien a afficher).</summary>
    private VisualElement? Render()
    {
        if (ResultSet is not { Rows.Count: > 0 } resultSet || resultSet.Columns.Count == 0)
            return null;

        var labelIndex = Math.Max(0, IndexOf(resultSet, LabelColumn));
        var valueIndexes = ValueIndexes(resultSet, labelIndex);
        if (valueIndexes.Count == 0)
            return NoValueLabel;

        var labels = resultSet.Rows.Select(r => Cell(r, labelIndex) ?? "NULL").ToArray();

        if (View == DashboardCardKind.PieChart)
        {
            var valueIndex = valueIndexes[0];
            Pie.Series = resultSet.Rows.Select((row, i) => (ISeries)new PieSeries<double>
            {
                Name = labels[i],
                Values = [Number(Cell(row, valueIndex))],
            }).ToArray();
            return Pie;
        }

        var series = valueIndexes.Select(index => View == DashboardCardKind.LineChart
            ? (ISeries)new LineSeries<double>
            {
                Name = resultSet.Columns[index].Name,
                Values = resultSet.Rows.Select(r => Number(Cell(r, index))).ToArray(),
                Fill = null,
            }
            : new ColumnSeries<double>
            {
                Name = resultSet.Columns[index].Name,
                Values = resultSet.Rows.Select(r => Number(Cell(r, index))).ToArray(),
            }).ToArray();

        Cartesian.Series = series;
        Cartesian.XAxes = [new Axis { Labels = labels, LabelsPaint = new SolidColorPaint(LabelColor) }];
        Cartesian.YAxes = [new Axis { LabelsPaint = new SolidColorPaint(LabelColor) }];
        Cartesian.LegendPosition = series.Length > 1 ? LegendPosition.Top : LegendPosition.Hidden;
        return Cartesian;
    }

    /// <summary>Colonnes choisies, sinon toutes celles dont la premiere ligne est un nombre.</summary>
    private List<int> ValueIndexes(QueryResultSet resultSet, int labelIndex)
    {
        var names = (ValueColumns ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Length > 0)
            return names.Select(n => IndexOf(resultSet, n)).Where(i => i >= 0).ToList();

        return Enumerable.Range(0, resultSet.Columns.Count)
            .Where(i => i != labelIndex && double.TryParse(Cell(resultSet.Rows[0], i), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            .ToList();
    }

    private static int IndexOf(QueryResultSet resultSet, string? column)
        => string.IsNullOrWhiteSpace(column)
            ? -1
            : resultSet.Columns.ToList().FindIndex(c => string.Equals(c.Name, column.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? Cell(QueryResultRow row, int index) => index < row.Cells.Length ? row.Cells[index] : null;

    private static double Number(string? text)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
