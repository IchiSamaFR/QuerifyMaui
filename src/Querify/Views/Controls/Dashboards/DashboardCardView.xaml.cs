using CommunityToolkit.Maui;
using Querify.Converters;
using Querify.Core.Models.Dashboards;
using Fluentia.Controls;
using System.ComponentModel;
using Querify.Diagnostics;
using Querify.UI.Extensions;
using Quill.Code.Execution;

namespace Querify.Views.Controls.Dashboards;

/// <summary>Une carte du dashboard : titre, puis tableau, graphique, valeur, texte ou bouton selon son rendu.</summary>
public partial class DashboardCardView : ContentView
{
    private static readonly QueryResultColumnsConverter ColumnsConverter = new();

    /// <summary>Incremente a chaque mise a jour : seule la derniere creation differee s'applique.</summary>
    private int _resultVersion;

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

        if (newCard is not null)
            newCard.PropertyChanged += OnCardStatePropertyChanged;

        // Tableau repris par une autre carte : le tri et les filtres etaient ceux de la precedente.
        if (newCard is not null && ResultHost.Content is DataGrid grid)
        {
            grid.ClearValue(DataGrid.SortMemberProperty);
            if (grid.ClearFiltersCommand?.CanExecute(null) == true)
                grid.ClearFiltersCommand.Execute(null);
        }

        UpdateResultView();
    }

    private void OnCardStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DashboardCardState.IsGrid) or nameof(DashboardCardState.IsChart))
            UpdateResultView();
    }

    /// <summary>
    /// Cree le seul rendu lourd dont la carte a besoin, et le garde tant que son type ne change pas : la vue
    /// est reutilisee d'une carte a l'autre (liaisons sur <see cref="Card"/>), un tableau ou un graphique deja
    /// cree sert donc la carte suivante. Le rendu est cree un tour plus tard : la carte s'affiche d'abord.
    /// </summary>
    private void UpdateResultView()
    {
        var version = ++_resultVersion;
        var content = ResultHost.Content;

        // Vue en reserve (sans carte) ou rendu deja du bon type : rien a creer.
        if (Card is not { } card || (card.IsGrid && content is DataGrid) || (card.IsChart && content is DashboardChartView))
            return;

        ResultHost.Content = null;
        if (card.IsGrid || card.IsChart)
            CreateResultViewLaterAsync(version).FireAndForget("Rendu de la carte non créé.");
    }

    private async Task CreateResultViewLaterAsync(int version)
    {
        await Task.Delay(1);

        // Carte changee ou rendu change entre-temps : une creation plus recente est prevue (ou plus rien).
        if (version != _resultVersion || Card is not { } card)
            return;

        if (card.IsGrid && ResultHost.Content is not DataGrid)
        {
            using (PerfTrace.Measure($"Carte '{card.Definition.Title}' : creation DataGrid"))
                ResultHost.Content = CreateGrid();
        }
        else if (card.IsChart && ResultHost.Content is not DashboardChartView)
        {
            using (PerfTrace.Measure($"Carte '{card.Definition.Title}' : creation graphique"))
                ResultHost.Content = CreateChart();
        }
    }

    // Liaisons sur Card (et non sur la carte du moment) : le rendu suit la carte quand la vue est reutilisee.
    private DataGrid CreateGrid()
    {
        var grid = new DataGrid { AutoGenerateColumns = false };
        grid.SetBinding(DataGrid.ItemsSourceProperty, new Binding($"{nameof(Card)}.{nameof(DashboardCardState.GridResult)}.{nameof(QueryResultSet.Rows)}", source: this));
        grid.SetBinding(DataGrid.ColumnsProperty, new Binding($"{nameof(Card)}.{nameof(DashboardCardState.GridResult)}", converter: ColumnsConverter, source: this));
        grid.SetBinding(DataGrid.ItemTappedCommandProperty, new Binding($"{nameof(Card)}.{nameof(DashboardCardState.ActivateRowCommand)}", source: this));
        return grid;
    }

    private DashboardChartView CreateChart()
    {
        var chart = new DashboardChartView();
        chart.SetBinding(DashboardChartView.ViewProperty, new Binding($"{nameof(Card)}.{nameof(DashboardCardState.View)}", source: this));
        chart.SetBinding(DashboardChartView.LabelColumnProperty, new Binding($"{nameof(Card)}.{nameof(DashboardCardState.LabelColumn)}", source: this));
        chart.SetBinding(DashboardChartView.ValueColumnsProperty, new Binding($"{nameof(Card)}.{nameof(DashboardCardState.ValueColumnsText)}", source: this));
        chart.SetBinding(DashboardChartView.ResultSetProperty, new Binding($"{nameof(Card)}.{nameof(DashboardCardState.Result)}", source: this));
        return chart;
    }
}
