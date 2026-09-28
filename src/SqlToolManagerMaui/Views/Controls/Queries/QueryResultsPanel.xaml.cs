using SqlToolManagerMaui.Models.Queries;

namespace SqlToolManagerMaui.Views.Controls.Queries;

/// <summary>
/// Panneau de resultats facon SSMS : un onglet par jeu de resultats et un onglet Messages
/// (PRINT, lignes affectees, erreurs).
/// </summary>
public partial class QueryResultsPanel : ContentView
{
    private const string MessagesTabTitle = "Messages";

    public QueryResultsPanel()
    {
        InitializeComponent();
    }

    private void OnResultChanged()
    {
        var result = Result;
        if (result is null)
        {
            SelectedTab = null;
            Tabs = [];
            return;
        }

        var messagesTab = new QueryResultsTab(MessagesTabTitle, ResultSet: null);
        Tabs = [.. result.ResultSets.Select(r => new QueryResultsTab(r.Title, r)), messagesTab];

        // Une erreur, ou l'absence de resultats, se lit dans les messages.
        SelectedTab = result.HasErrors || result.WasCancelled || result.ResultSets.Count == 0
            ? messagesTab
            : Tabs[0];
    }
}
