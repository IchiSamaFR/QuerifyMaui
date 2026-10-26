namespace Querify.UI.Views.Controls.DataGrids;

/// <summary>
/// Ligne du <see cref="DataGrid"/>. Les colonnes n'etant connues qu'a l'execution, les cellules sont creees ici
/// et alimentees au changement de contexte (recyclage) plutot que par des bindings, trop couteux et non
/// compilables sur un nombre variable de colonnes.
/// </summary>
public sealed partial class DataGridRowView : Grid
{
    private const string CellStyleKey = "DataGridCellStyle";
    private const string NullCellStyleKey = "DataGridNullCellStyle";
    private const string LineColorKey = "DataGridLineColor";
    private const string AlternateBackgroundKey = "DataGridAlternateRowBackground";

    private readonly DataGrid _owner;
    private readonly IReadOnlyList<DataGridColumn> _columns;
    private readonly string _nullText;
    private readonly Label[] _cells;
    private readonly bool[] _isNullStyled;
    private bool _isAlternate;

    public DataGridRowView(DataGrid owner, IReadOnlyList<DataGridColumn> columns, IReadOnlyList<double> widths, string nullText)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(widths);

        InitializeComponent();

        _owner = owner;
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

            // Trait vertical dans l'espacement de 1 qui suit la colonne ; le cadre ferme la derniere.
            if (i < columns.Count - 1)
                this.Add(CreateLine(widthRequest: 1, horizontalOptions: LayoutOptions.End, translationX: 1), i);
        }

        // Trait horizontal dans la marge basse de 1 de la ligne.
        var bottom = CreateLine(heightRequest: 1, verticalOptions: LayoutOptions.End, translationY: 1);
        this.Add(bottom, 0);
        Grid.SetColumnSpan(bottom, Math.Max(1, columns.Count));

        owner.RowsReordered += OnRowsReordered;
    }

    private static BoxView CreateLine(double widthRequest = -1, double heightRequest = -1, LayoutOptions? horizontalOptions = null,
        LayoutOptions? verticalOptions = null, double translationX = 0, double translationY = 0)
    {
        var line = new BoxView
        {
            WidthRequest = widthRequest,
            HeightRequest = heightRequest,
            HorizontalOptions = horizontalOptions ?? LayoutOptions.Fill,
            VerticalOptions = verticalOptions ?? LayoutOptions.Fill,
            TranslationX = translationX,
            TranslationY = translationY,
            InputTransparent = true,
        };
        line.SetDynamicResource(BoxView.ColorProperty, LineColorKey);
        return line;
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is not { } item)
            return;

        UpdateStripe();

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

    private void OnRowsReordered(object? sender, EventArgs e) => UpdateStripe();

    /// <summary>Une ligne sur deux (position affichee, apres filtre et tri) est assombrie.</summary>
    private void UpdateStripe()
    {
        var isAlternate = BindingContext is { } item && _owner.IndexOfRow(item) % 2 == 1;
        if (isAlternate == _isAlternate)
            return;

        _isAlternate = isAlternate;
        if (isAlternate)
            SetDynamicResource(BackgroundColorProperty, AlternateBackgroundKey);
        else
            BackgroundColor = Colors.Transparent;
    }
}
