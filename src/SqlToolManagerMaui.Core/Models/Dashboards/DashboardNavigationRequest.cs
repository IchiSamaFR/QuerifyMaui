
using SqlToolManagerMaui.Databases.Models.Queries;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>Clic sur un bouton, ou sur une ligne (<paramref name="Row"/>) d'un tableau qui ouvre une page.</summary>
public sealed record DashboardNavigationRequest(DashboardCardState Card, QueryResultRow? Row);
