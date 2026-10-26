using Querify.Databases.Models.Queries;
using Querify.UI.Views.Controls.DataGrids;
using System.Globalization;

namespace Querify.Converters;

/// <summary>
/// Colonnes d'un <see cref="QueryResultSet"/> pour <see cref="DataGrid.Columns"/> : connues seulement a l'execution,
/// elles lisent les cellules par position (sans reflexion).
/// </summary>
public sealed class QueryResultColumnsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is QueryResultSet resultSet
            ? resultSet.Columns.Select(CreateColumn).ToList()
            : new List<DataGridColumn>();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static DataGridColumn CreateColumn(QueryResultColumn column, int index) => new()
    {
        Header = column.Name,
        ToolTip = $"{column.Name} ({column.TypeName})",
        ValueGetter = item => item is QueryResultRow row && index < row.Cells.Length ? row.Cells[index] : null,
    };
}
