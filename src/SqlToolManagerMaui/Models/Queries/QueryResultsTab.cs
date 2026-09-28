namespace SqlToolManagerMaui.Models.Queries;

/// <summary>
/// Onglet du panneau de resultats : soit un jeu de resultats, soit la liste des messages.
/// </summary>
public sealed record QueryResultsTab(string Title, QueryResultSet? ResultSet)
{
    public bool IsResultSet => ResultSet is not null;

    public bool IsMessages => ResultSet is null;
}
