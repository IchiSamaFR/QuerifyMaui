using System.Windows.Input;
using SqlToolManagerMaui.Models.Queries;

namespace SqlToolManagerMaui.Views.Controls.Queries;

public partial class QueryResultsPanel
{
    public static readonly BindableProperty ResultProperty = BindableProperty.Create(
        nameof(Result), typeof(QueryExecutionResult), typeof(QueryResultsPanel), null,
        propertyChanged: (bindable, _, _) => ((QueryResultsPanel)bindable).OnResultChanged());

    /// <summary>Resultat affiche ; null pendant une execution ou avant la premiere.</summary>
    public QueryExecutionResult? Result
    {
        get => (QueryExecutionResult?)GetValue(ResultProperty);
        set => SetValue(ResultProperty, value);
    }

    public static readonly BindableProperty IsExecutingProperty = BindableProperty.Create(
        nameof(IsExecuting), typeof(bool), typeof(QueryResultsPanel), false);

    /// <summary>Affiche l'indicateur d'execution en cours.</summary>
    public bool IsExecuting
    {
        get => (bool)GetValue(IsExecutingProperty);
        set => SetValue(IsExecutingProperty, value);
    }

    public static readonly BindableProperty CloseCommandProperty = BindableProperty.Create(
        nameof(CloseCommand), typeof(ICommand), typeof(QueryResultsPanel), null);

    /// <summary>Ferme le panneau.</summary>
    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    private static readonly BindablePropertyKey TabsPropertyKey = BindableProperty.CreateReadOnly(
        nameof(Tabs), typeof(IReadOnlyList<QueryResultsTab>), typeof(QueryResultsPanel), Array.Empty<QueryResultsTab>());

    public static readonly BindableProperty TabsProperty = TabsPropertyKey.BindableProperty;

    /// <summary>Un onglet par jeu de resultats, puis l'onglet Messages.</summary>
    public IReadOnlyList<QueryResultsTab> Tabs
    {
        get => (IReadOnlyList<QueryResultsTab>)GetValue(TabsProperty);
        private set => SetValue(TabsPropertyKey, value);
    }

    public static readonly BindableProperty SelectedTabProperty = BindableProperty.Create(
        nameof(SelectedTab), typeof(QueryResultsTab), typeof(QueryResultsPanel), null, BindingMode.TwoWay);

    /// <summary>Onglet affiche.</summary>
    public QueryResultsTab? SelectedTab
    {
        get => (QueryResultsTab?)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }
}
