using SqlToolManagerMaui.Core.Enums.Dashboards;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>Parametre envoye avec la requete d'une carte (null = NULL SQL).</summary>
public sealed record DashboardQueryParameter(string Name, DashboardParameterType Type, object? Value);
