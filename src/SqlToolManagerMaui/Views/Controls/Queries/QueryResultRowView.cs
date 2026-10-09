using SqlToolManagerMaui.Core.Models.Queries;
using System.Globalization;

namespace SqlToolManagerMaui.Views.Controls.Queries;

/// <summary>
/// Ligne de la grille de resultats. Les colonnes etant connues seulement a l'execution,
/// les cellules sont creees ici et alimentees au changement de contexte (recyclage) plutot
/// que par des bindings, trop couteux et non compilables sur un nombre variable de colonnes.
/// </summary>
public sealed class QueryResultRowView : Grid
{
    private const string CellStyleKey = "QueryResultCellStyle";
    private const string NullCellStyleKey = "QueryResultNullCellStyle";
    private const string NullText = "NULL";

    private readonly Label _number;
    private readonly Label[] _cells;
    private readonly bool[] _isNullStyled;

    public QueryResultRowView(double rowNumberWidth, IReadOnlyList<double> columnWidths)
    {
        ArgumentNullException.ThrowIfNull(columnWidths);

        SetDynamicResource(StyleProperty, "QueryResultRowStyle");

        ColumnDefinitions.Add(new ColumnDefinition(rowNumberWidth));
        _number = new Label();
        _number.SetDynamicResource(Label.StyleProperty, "QueryResultRowNumberStyle");
        this.Add(_number, 0);

        _cells = new Label[columnWidths.Count];
        _isNullStyled = new bool[columnWidths.Count];
        for (var i = 0; i < columnWidths.Count; i++)
        {
            ColumnDefinitions.Add(new ColumnDefinition(columnWidths[i]));

            var cell = new Label();
            cell.SetDynamicResource(Label.StyleProperty, CellStyleKey);
            this.Add(cell, i + 1);
            _cells[i] = cell;
        }
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is not QueryResultRow row)
            return;

        _number.Text = row.Number.ToString(CultureInfo.InvariantCulture);

        for (var i = 0; i < _cells.Length; i++)
        {
            var value = i < row.Cells.Length ? row.Cells[i] : null;
            var isNull = value is null;

            _cells[i].Text = value ?? NullText;

            // Le style ne change qu'au passage NULL / non NULL, pas a chaque recyclage.
            if (_isNullStyled[i] != isNull)
            {
                _cells[i].SetDynamicResource(Label.StyleProperty, isNull ? NullCellStyleKey : CellStyleKey);
                _isNullStyled[i] = isNull;
            }
        }
    }
}
