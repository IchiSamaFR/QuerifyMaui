using CommunityToolkit.Maui;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Querify.Core.Enums.Dashboards;
using Querify.Databases.Models.Queries;
using System.Globalization;

namespace Querify.Views.Controls.Dashboards;

/// <summary>
/// Graphique d'une carte (LiveCharts2). Les series dependent des colonnes du resultat, connues
/// seulement a l'execution : elles sont recalculees en code, une seule fois par lot de changements.
/// </summary>
public sealed partial class DashboardChartView : ContentView
{
    // Gris lisible sur les deux themes : les axes ne suivent pas les ressources MAUI.
    private static readonly SKColor LabelColor = new(0x9A, 0x9A, 0x9A);

    /// <summary>Au-dela, les parts sont illisibles et chaque part est une serie : le reste est regroupe.</summary>
    private const int MaxPieSlices = 12;

    private const string OtherSliceName = "Autres";

    /// <summary>Au-dela, les points d'une courbe ne sont plus dessines (des milliers de geometries sinon).</summary>
    private const int MaxLinePointsWithMarkers = 60;

    private PieChart? _pie;
    private CartesianChart? _cartesian;
    private bool _isRenderScheduled;

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
    }

    /// <summary>
    /// Les quatre proprietes arrivent ensemble (liaison initiale, nouveau resultat) :
    /// un seul rendu au prochain tour du dispatcher au lieu de quatre.
    /// </summary>
    private static void OnChartChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DashboardChartView)bindable).ScheduleRender();

    private void ScheduleRender()
    {
        if (_isRenderScheduled)
            return;

        _isRenderScheduled = true;
        Dispatcher.Dispatch(() =>
        {
            _isRenderScheduled = false;
            Show(Render());
        });
    }

    private void Show(VisualElement? visible)
    {
        NoValueLabel.IsVisible = visible == NoValueLabel;
        if (_pie is not null)
            _pie.IsVisible = visible == _pie;
        if (_cartesian is not null)
            _cartesian.IsVisible = visible == _cartesian;
    }

    // ---------- Graphiques crees a la demande ----------

    private PieChart Pie => _pie ??= AddChart(new PieChart
    {
        LegendPosition = LegendPosition.Right,
        LegendTextPaint = new SolidColorPaint(LabelColor),
    });

    private CartesianChart Cartesian => _cartesian ??= AddChart(new CartesianChart
    {
        LegendTextPaint = new SolidColorPaint(LabelColor),
    });

    /// <summary>Sans animation : chaque lecture de carte relancait une animation par serie et par point.</summary>
    private T AddChart<T>(T chart) where T : Microsoft.Maui.Controls.View
    {
        chart.Background = Brush.Transparent;
        chart.IsVisible = false;

        switch (chart)
        {
            case PieChart pie:
                pie.EasingFunction = null;
                pie.AnimationsSpeed = TimeSpan.Zero;
                break;
            case CartesianChart cartesian:
                cartesian.EasingFunction = null;
                cartesian.AnimationsSpeed = TimeSpan.Zero;
                break;
        }

        Root.Add(chart);
        return chart;
    }

    // ---------- Rendu ----------

    /// <summary>Alimente le rendu adapte et le renvoie (null : rien a afficher).</summary>
    private VisualElement? Render()
    {
        if (ResultSet is not { Rows.Count: > 0 } resultSet || resultSet.Columns.Count == 0)
            return null;

        var labelIndex = Math.Max(0, IndexOf(resultSet, LabelColumn));
        var valueIndexes = ValueIndexes(resultSet, labelIndex);
        if (valueIndexes.Count == 0)
            return NoValueLabel;

        var rows = resultSet.Rows;
        var labels = new string[rows.Count];
        for (var i = 0; i < rows.Count; i++)
            labels[i] = Cell(rows[i], labelIndex) ?? "NULL";

        if (View == DashboardCardKind.PieChart)
        {
            Pie.Series = BuildPieSeries(rows, labels, valueIndexes[0]);
            return Pie;
        }

        var isLine = View == DashboardCardKind.LineChart;
        var series = new ISeries[valueIndexes.Count];
        for (var s = 0; s < valueIndexes.Count; s++)
        {
            var index = valueIndexes[s];
            var values = new double[rows.Count];
            for (var i = 0; i < rows.Count; i++)
                values[i] = Number(Cell(rows[i], index));

            series[s] = isLine
                ? new LineSeries<double>
                {
                    Name = resultSet.Columns[index].Name,
                    Values = values,
                    Fill = null,
                    GeometrySize = rows.Count > MaxLinePointsWithMarkers ? 0 : 8,
                }
                : new ColumnSeries<double>
                {
                    Name = resultSet.Columns[index].Name,
                    Values = values,
                };
        }

        var chart = Cartesian;
        chart.Series = series;
        chart.XAxes = [new Axis { Labels = labels, LabelsPaint = new SolidColorPaint(LabelColor) }];
        chart.YAxes = [new Axis { LabelsPaint = new SolidColorPaint(LabelColor) }];
        chart.LegendPosition = series.Length > 1 ? LegendPosition.Top : LegendPosition.Hidden;
        return chart;
    }

    /// <summary>Une serie par part : les plus grosses parts gardent leur nom, le reste est cumule dans « Autres ».</summary>
    private static ISeries[] BuildPieSeries(IReadOnlyList<QueryResultRow> rows, string[] labels, int valueIndex)
    {
        var slices = new (string Name, double Value)[rows.Count];
        for (var i = 0; i < rows.Count; i++)
            slices[i] = (labels[i], Number(Cell(rows[i], valueIndex)));

        if (slices.Length > MaxPieSlices)
        {
            var ordered = slices.OrderByDescending(s => s.Value).ToArray();
            var other = ordered.Skip(MaxPieSlices - 1).Sum(s => s.Value);
            slices = [.. ordered.Take(MaxPieSlices - 1), (OtherSliceName, other)];
        }

        return slices.Select(slice => (ISeries)new PieSeries<double>
        {
            Name = slice.Name,
            Values = [slice.Value],
        }).ToArray();
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
    {
        if (string.IsNullOrWhiteSpace(column))
            return -1;

        var name = column.Trim();
        for (var i = 0; i < resultSet.Columns.Count; i++)
        {
            if (string.Equals(resultSet.Columns[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static string? Cell(QueryResultRow row, int index) => index < row.Cells.Length ? row.Cells[index] : null;

    private static double Number(string? text)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
}
