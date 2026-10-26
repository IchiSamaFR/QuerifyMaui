namespace Querify.Core.Models.Dashboards;

/// <summary>Position et etendue d'une carte dans la grille, en cases.</summary>
public sealed record DashboardCardPlacement(int Column, int Row, int ColumnSpan, int RowSpan);
