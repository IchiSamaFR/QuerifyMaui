using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Models.Queries;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Queries;

/// <summary>
/// Panneau de resultats facon SSMS : un onglet par jeu de resultats et un onglet Messages
/// (PRINT, lignes affectees, erreurs).
/// </summary>
public partial class QueryResultsPanel : ContentView
{
    private const string MESSAGES_TAB_TITLE = "Messages";

    /// <summary>Resultat affiche ; null pendant une execution ou avant la premiere.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnResultChanged))]
    public partial QueryExecutionResult? Result { get; set; }

    /// <summary>Affiche l'indicateur d'execution en cours.</summary>
    [BindableProperty]
    public partial bool IsExecuting { get; set; }

    /// <summary>Ferme le panneau.</summary>
    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    /// <summary>Un onglet par jeu de resultats, puis l'onglet Messages.</summary>
    [BindableProperty]
    public partial IReadOnlyList<QueryResultsTab> Tabs { get; private set; } = Array.Empty<QueryResultsTab>();

    /// <summary>Onglet affiche.</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial QueryResultsTab? SelectedTab { get; set; }

    public QueryResultsPanel()
    {
        InitializeComponent();
    }

    static void OnResultChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not QueryResultsPanel panel)
            return;

        var result = (QueryExecutionResult?)newValue;
        if (result is null)
        {
            panel.SelectedTab = null;
            panel.Tabs = [];
            return;
        }

        var messagesTab = new QueryResultsTab(MESSAGES_TAB_TITLE, ResultSet: null);
        panel.Tabs = [.. result.ResultSets.Select(r => new QueryResultsTab(r.Title, r)), messagesTab];

        // Une erreur, ou l'absence de resultats, se lit dans les messages.
        panel.SelectedTab = result.HasErrors || result.WasCancelled || result.ResultSets.Count == 0
            ? messagesTab
            : panel.Tabs[0];
    }
}
