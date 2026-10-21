using SqlToolManagerMaui.Core.Models.Dashboards;

namespace SqlToolManagerMaui.Views.Controls.Dashboards;

/// <summary>
/// Cases libres du concepteur dessinees sur un seul canevas : une vue par case (plus de 80) figeait
/// l'ouverture du concepteur. Meme geometrie que le calque des cartes (colonnes en *, lignes fixes).
/// </summary>
public sealed class DashboardCellsDrawable : IDrawable
{
    public int ColumnCount { get; set; } = 12;
    public int RowCount { get; set; } = 6;
    public double RowHeight { get; set; } = 140d;
    public double ColumnSpacing { get; set; } = 8d;
    public double RowSpacing { get; set; } = 8d;
    public float CornerRadius { get; set; } = 4f;

    /// <summary>Case sous le pointeur, surlignee.</summary>
    public DashboardCell? HoveredCell { get; set; }

    public Color StrokeColor { get; set; } = Colors.Gray;
    public Color HoverColor { get; set; } = Colors.Transparent;
    public Color GlyphColor { get; set; } = Colors.Gray;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var columnWidth = GetColumnWidth(dirtyRect.Width);
        if (columnWidth <= 0)
            return;

        canvas.StrokeColor = StrokeColor;
        canvas.StrokeSize = 0;
        canvas.FontColor = GlyphColor;
        canvas.FontSize = 14;

        for (var row = 0; row < RowCount; row++)
        {
            for (var column = 0; column < ColumnCount; column++)
            {
                var origin = GetOrigin(column, row, columnWidth);
                var bounds = new RectF((float)origin.X, (float)origin.Y, (float)columnWidth, (float)RowHeight);

                if (HoveredCell is { } hovered && hovered.Column == column && hovered.Row == row)
                {
                    canvas.FillColor = HoverColor;
                    canvas.FillRoundedRectangle(bounds, CornerRadius);
                }

                // Demi-pixel : le trait de 1 px reste net et dans la case.
                canvas.DrawRoundedRectangle(bounds.Inflate(-0.5f, -0.5f), CornerRadius);
                canvas.DrawString("+", bounds, Microsoft.Maui.Graphics.HorizontalAlignment.Center, Microsoft.Maui.Graphics.VerticalAlignment.Center);
            }
        }
    }

    /// <summary>Case sous <paramref name="point"/> ; l'espacement qui suit une case lui est rattache (pas de trou au glisser).</summary>
    public DashboardCell? HitTest(Point point, double width)
    {
        var columnWidth = GetColumnWidth(width);
        if (columnWidth <= 0 || point.X < 0 || point.Y < 0)
            return null;

        var column = (int)(point.X / (columnWidth + ColumnSpacing));
        var row = (int)(point.Y / (RowHeight + RowSpacing));
        if (row >= RowCount)
            return null;

        return new DashboardCell(Math.Min(column, ColumnCount - 1), row);
    }

    /// <summary>Coin haut-gauche de la case, pour un canevas de largeur <paramref name="width"/>.</summary>
    public Point GetCellOrigin(int column, int row, double width)
        => GetOrigin(column, row, GetColumnWidth(width));

    private Point GetOrigin(int column, int row, double columnWidth)
        => new(column * (columnWidth + ColumnSpacing), row * (RowHeight + RowSpacing));

    /// <summary>Case la plus proche du coin haut-gauche <paramref name="topLeft"/>, ramenee dans la grille.</summary>
    public DashboardCell? SnapCell(Point topLeft, double width)
    {
        var columnWidth = GetColumnWidth(width);
        if (columnWidth <= 0)
            return null;

        var column = (int)Math.Round(topLeft.X / (columnWidth + ColumnSpacing));
        var row = (int)Math.Round(topLeft.Y / (RowHeight + RowSpacing));
        return new DashboardCell(Math.Clamp(column, 0, ColumnCount - 1), Math.Clamp(row, 0, RowCount - 1));
    }

    private double GetColumnWidth(double width)
        => ColumnCount <= 0 ? 0 : (width - (ColumnCount - 1) * ColumnSpacing) / ColumnCount;
}
