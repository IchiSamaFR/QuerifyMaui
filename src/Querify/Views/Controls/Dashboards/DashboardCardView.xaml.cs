using CommunityToolkit.Maui;
using Querify.Converters;
using Querify.Core.Models.Dashboards;
using Querify.Databases.Models.Queries;
using Querify.UI.Views.Controls.DataGrids;
using System.ComponentModel;

namespace Querify.Views.Controls.Dashboards;

/// <summary>Une carte du dashboard : titre, puis tableau, graphique, valeur, texte ou bouton selon son rendu.</summary>
public partial class DashboardCardView : ContentView
{
    private static readonly QueryResultColumnsConverter ColumnsConverter = new();

    [BindableProperty(PropertyChangedMethodName = nameof(OnCardPropertyChanged))]
    public partial DashboardCardState? Card { get; set; }

    public DashboardCardView()
    {
        InitializeComponent();
    }

    private static void OnCardPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DashboardCardView)bindable).OnCardChanged(oldValue as DashboardCardState, newValue as DashboardCardState);

    private void OnCardChanged(DashboardCardState? oldCard, DashboardCardState? newCard)
    {
        if (oldCard is not null)
            oldCard.PropertyChanged -= OnCardStatePropertyChanged;

        // Le rendu precedent est lie a l'ancienne carte : il est jete.
        ResultHost.Content = null;

        if (newCard is not null)
            newCard.PropertyChanged += OnCardStatePropertyChanged;

        UpdateResultView();
    }

    private void OnCardStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DashboardCardState.IsGrid) or nameof(DashboardCardState.IsChart))
            UpdateResultView();
    }

    /// <summary>Cree le seul rendu lourd dont la carte a besoin, et le garde tant que son type ne change pas.</summary>
    private void UpdateResultView()
    {
        switch (Card)
        {
            case { IsGrid: true } card when ResultHost.Content is not DataGrid:
                ResultHost.Content = CreateGrid(card);
                break;
            case { IsChart: true } card when ResultHost.Content is not DashboardChartView:
                ResultHost.Content = CreateChart(card);
                break;
            case { IsGrid: false, IsChart: false } or null:
                ResultHost.Content = null;
                break;
        }
    }

    private static DataGrid CreateGrid(DashboardCardState card)
    {
        var grid = new DataGrid { AutoGenerateColumns = false };
        grid.SetBinding(DataGrid.ItemsSourceProperty, new Binding($"{nameof(DashboardCardState.GridResult)}.{nameof(QueryResultSet.Rows)}", source: card));
        grid.SetBinding(DataGrid.ColumnsProperty, new Binding(nameof(DashboardCardState.GridResult), converter: ColumnsConverter, source: card));
        grid.SetBinding(DataGrid.ItemTappedCommandProperty, new Binding(nameof(DashboardCardState.ActivateRowCommand), source: card));
        return grid;
    }

    private static DashboardChartView CreateChart(DashboardCardState card)
    {
        var chart = new DashboardChartView();
        chart.SetBinding(DashboardChartView.ViewProperty, new Binding(nameof(DashboardCardState.View), source: card));
        chart.SetBinding(DashboardChartView.LabelColumnProperty, new Binding(nameof(DashboardCardState.LabelColumn), source: card));
        chart.SetBinding(DashboardChartView.ValueColumnsProperty, new Binding(nameof(DashboardCardState.ValueColumnsText), source: card));
        chart.SetBinding(DashboardChartView.ResultSetProperty, new Binding(nameof(DashboardCardState.Result), source: card));
        return chart;
    }
}
