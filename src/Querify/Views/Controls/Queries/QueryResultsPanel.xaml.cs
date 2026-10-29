using CommunityToolkit.Maui;
using Querify.Core.Models.Queries;
using System.Windows.Input;

namespace Querify.Views.Controls.Queries;

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
    [BindableProperty(PropertyChangedMethodName = nameof(OnLiveStateChanged))]
    public partial bool IsExecuting { get; set; }

    /// <summary>Messages recus pendant l'execution (Log du script), affiches avant le resultat.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnLiveStateChanged))]
    public partial IReadOnlyList<QueryMessage>? LiveMessages { get; set; }

    /// <summary>Messages de l'onglet Messages : ceux de l'execution en cours, puis ceux du resultat.</summary>
    [BindableProperty]
    public partial IReadOnlyList<QueryMessage>? DisplayedMessages { get; private set; }

    /// <summary>Execution en cours sans message a montrer encore : l'indicateur seul.</summary>
    [BindableProperty]
    public partial bool IsWaiting { get; private set; }

    /// <summary>Ferme le panneau.</summary>
    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    /// <summary>L'onglet Messages, puis un onglet par jeu de resultats.</summary>
    [BindableProperty]
    public partial IReadOnlyList<QueryResultsTab> Tabs { get; private set; } = Array.Empty<QueryResultsTab>();

    /// <summary>Onglet affiche.</summary>
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial QueryResultsTab? SelectedTab { get; set; }

    // Toujours present et en premier, meme avant le resultat : il recoit le Log pendant l'execution.
    private readonly QueryResultsTab _messagesTab = new(MESSAGES_TAB_TITLE, ResultSet: null);

    public QueryResultsPanel()
    {
        Tabs = [_messagesTab];
        SelectedTab = _messagesTab;
        InitializeComponent();
    }

    static void OnLiveStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not QueryResultsPanel panel)
            return;

        panel.RefreshMessages();
    }

    private void RefreshMessages()
    {
        DisplayedMessages = Result?.Messages ?? LiveMessages;
        IsWaiting = IsExecuting && DisplayedMessages is not { Count: > 0 };
    }

    static void OnResultChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not QueryResultsPanel panel)
            return;

        panel.RefreshMessages();

        var result = (QueryExecutionResult?)newValue;
        if (result is null)
        {
            // Nouvelle execution : seuls les messages restent, vides.
            panel.Tabs = [panel._messagesTab];
            panel.SelectedTab = panel._messagesTab;
            return;
        }

        panel.Tabs = [panel._messagesTab, .. result.ResultSets.Select(r => new QueryResultsTab(r.Title, r))];

        // Une erreur, ou l'absence de resultats, se lit dans les messages.
        panel.SelectedTab = result.HasErrors || result.WasCancelled || result.ResultSets.Count == 0
            ? panel._messagesTab
            : panel.Tabs[1];
    }
}
