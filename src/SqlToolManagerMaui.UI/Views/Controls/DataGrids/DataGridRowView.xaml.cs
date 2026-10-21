namespace SqlToolManagerMaui.UI.Views.Controls.DataGrids;

/// <summary>
/// Ligne du <see cref="DataGrid"/>. Les colonnes n'etant connues qu'a l'execution, les cellules sont creees ici
/// et alimentees au changement de contexte (recyclage) plutot que par des bindings, trop couteux et non
/// compilables sur un nombre variable de colonnes.
/// </summary>
public sealed partial class DataGridRowView : Grid
{
    private const string CellStyleKey = "DataGridCellStyle";
    private const string NullCellStyleKey = "DataGridNullCellStyle";

    private readonly IReadOnlyList<DataGridColumn> _columns;
    private readonly string _nullText;
    private readonly Label[] _cells;
    private readonly bool[] _isNullStyled;

    public DataGridRowView(IReadOnlyList<DataGridColumn> columns, IReadOnlyList<double> widths, string nullText)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(widths);

        InitializeComponent();

        _columns = columns;
        _nullText = nullText;

        _cells = new Label[columns.Count];
        _isNullStyled = new bool[columns.Count];
        for (var i = 0; i < columns.Count; i++)
        {
            ColumnDefinitions.Add(new ColumnDefinition(widths[i]));

            var cell = new Label();
            cell.SetDynamicResource(Label.StyleProperty, CellStyleKey);
            this.Add(cell, i);
            _cells[i] = cell;
        }
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is not { } item)
            return;

        for (var i = 0; i < _cells.Length; i++)
        {
            var text = _columns[i].FormatValue(_columns[i].ReadValue(item));
            var isNull = text is null;

            _cells[i].Text = text ?? _nullText;

            // Le style ne change qu'au passage NULL / non NULL, pas a chaque recyclage.
            if (_isNullStyled[i] != isNull)
            {
                _cells[i].SetDynamicResource(Label.StyleProperty, isNull ? NullCellStyleKey : CellStyleKey);
                _isNullStyled[i] = isNull;
            }
        }
    }
}
