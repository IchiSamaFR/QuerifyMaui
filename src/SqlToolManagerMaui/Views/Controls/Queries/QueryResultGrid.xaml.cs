using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Models.Queries;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Queries;

/// <summary>
/// Grille en lecture seule d'un jeu de resultats : en-tete fixe, lignes virtualisees,
/// defilement horizontal quand les colonnes depassent la largeur disponible.
/// Les colonnes ne sont connues qu'a l'execution : elles sont posees en code dans le squelette XAML.
/// </summary>
public sealed partial class QueryResultGrid : ContentView
{
    private const string HeaderStyleKey = "QueryResultHeaderStyle";
    private const double RowNumberWidth = 56;
    private const double MinColumnWidth = 60;
    private const double MaxColumnWidth = 360;
    private const double ApproximateCharWidth = 7.5;
    private const double CellHorizontalPadding = 24;

    /// <summary>Nombre de lignes echantillonnees pour dimensionner les colonnes.</summary>
    private const int WidthSampleRowCount = 200;

    /// <summary>Jeu de resultats affiche.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnResultSetChanged))]
    public partial QueryResultSet? ResultSet { get; set; }

    /// <summary>Clic sur une ligne (parametre : <see cref="QueryResultRow"/>) ; sans commande, les lignes ne sont pas selectionnables.</summary>
    [BindableProperty]
    public partial ICommand? RowTappedCommand { get; set; }

    public QueryResultGrid()
    {
        InitializeComponent();
    }

    private static void OnResultSetChanged(BindableObject bindable, object oldValue, object newValue)
        => ((QueryResultGrid)bindable).Rebuild();

    private void Rebuild()
    {
        // Lignes videes avant le changement de gabarit : les lignes recyclees portent les anciennes largeurs.
        Rows.ItemsSource = null;
        Header.Clear();
        Header.ColumnDefinitions.Clear();

        if (ResultSet is not { } resultSet)
            return;

        var widths = ComputeColumnWidths(resultSet);

        Header.ColumnDefinitions.Add(new ColumnDefinition(RowNumberWidth));
        Header.Add(CreateHeaderCell(), 0);
        for (var i = 0; i < resultSet.Columns.Count; i++)
        {
            var column = resultSet.Columns[i];
            Header.ColumnDefinitions.Add(new ColumnDefinition(widths[i]));
            Header.Add(CreateHeaderCell(column.Name, $"{column.Name} ({column.TypeName})"), i + 1);
        }

        Rows.WidthRequest = RowNumberWidth + widths.Sum() + widths.Count;
        Rows.ItemTemplate = new DataTemplate(() => new QueryResultRowView(RowNumberWidth, widths));
        Rows.ItemsSource = resultSet.Rows;
    }

    private void OnRowsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not QueryResultRow row)
            return;

        // Deselection immediate : un nouveau clic sur la meme ligne doit relancer la commande.
        Rows.SelectedItem = null;
        if (RowTappedCommand is { } command && command.CanExecute(row))
            command.Execute(row);
    }

    private static Label CreateHeaderCell(string? text = null, string? toolTip = null)
    {
        var label = new Label { Text = text };
        label.SetDynamicResource(Label.StyleProperty, HeaderStyleKey);
        if (toolTip is not null)
            ToolTipProperties.SetText(label, toolTip);
        return label;
    }

    /// <summary>Largeur estimee d'apres le plus long texte (en-tete ou echantillon de lignes), bornee.</summary>
    private static List<double> ComputeColumnWidths(QueryResultSet resultSet)
    {
        var sample = resultSet.Rows.Take(WidthSampleRowCount).ToList();
        var widths = new List<double>(resultSet.Columns.Count);

        for (var i = 0; i < resultSet.Columns.Count; i++)
        {
            var longest = resultSet.Columns[i].Name.Length;
            foreach (var row in sample)
            {
                var length = i < row.Cells.Length ? row.Cells[i]?.Length ?? 4 : 0;
                if (length > longest)
                    longest = length;
            }

            widths.Add(Math.Clamp((longest * ApproximateCharWidth) + CellHorizontalPadding, MinColumnWidth, MaxColumnWidth));
        }

        return widths;
    }
}
