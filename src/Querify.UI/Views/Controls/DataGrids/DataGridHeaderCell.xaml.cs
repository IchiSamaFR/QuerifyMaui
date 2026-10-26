using CommunityToolkit.Maui;
using System.Windows.Input;

namespace Querify.UI.Views.Controls.DataGrids;

/// <summary>En-tete d'une colonne du <see cref="DataGrid"/> : sens du tri, titre (clic pour trier), bouton de filtre.</summary>
public partial class DataGridHeaderCell : ContentView
{
    [BindableProperty]
    public partial DataGridColumn? Column { get; set; }

    /// <summary>Execute avec <see cref="Column"/> au clic sur le titre.</summary>
    [BindableProperty]
    public partial ICommand? SortCommand { get; set; }

    /// <summary>Execute avec <see cref="Column"/> au clic sur le bouton de filtre.</summary>
    [BindableProperty]
    public partial ICommand? FilterCommand { get; set; }

    [BindableProperty]
    public partial bool IsFilterButtonVisible { get; set; }

    public DataGridHeaderCell()
    {
        InitializeComponent();
    }
}
