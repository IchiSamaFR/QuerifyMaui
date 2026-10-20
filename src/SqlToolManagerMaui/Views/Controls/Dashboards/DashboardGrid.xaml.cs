using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Models.Dashboards;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Dashboards;

/// <summary>
/// Dashboard d'un onglet .schx : parametres, fil d'Ariane et grille de cartes. Dans le concepteur,
/// les cases libres permettent d'ajouter une carte ou d'y deposer la carte glissee.
/// </summary>
public partial class DashboardGrid : ContentView
{
    [BindableProperty]
    public partial DashboardSession? Session { get; set; }

    [BindableProperty]
    public partial bool IsDashboardParamsOpen { get; set; }

    /// <summary>Nombre de lignes de la grille (les definitions de lignes sont creees en code).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnGridSizeChanged))]
    public partial int RowCount { get; set; } = 6;

    [BindableProperty(PropertyChangedMethodName = nameof(OnGridSizeChanged))]
    public partial int ColumnCount { get; set; } = 12;

    /// <summary>Hauteur d'une ligne de la grille.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnGridSizeChanged))]
    public partial double RowHeight { get; set; } = 140d;

    /// <summary>Applique les valeurs saisies et relit les cartes.</summary>
    [BindableProperty]
    public partial ICommand? ApplyParametersCommand { get; set; }

    [BindableProperty]
    public partial ICommand? BackCommand { get; set; }

    /// <summary>Revient a une etape du fil d'Ariane (parametre : <see cref="DashboardPageState"/>).</summary>
    [BindableProperty]
    public partial ICommand? GoToStepCommand { get; set; }

    /// <summary>Concepteur : selectionne une carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? SelectCardCommand { get; set; }

    /// <summary>Concepteur : ajoute une carte dans une case libre (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? AddCardCommand { get; set; }

    /// <summary>Concepteur : debut du glisser d'une carte (parametre : <see cref="DashboardCardState"/>).</summary>
    [BindableProperty]
    public partial ICommand? StartDragCommand { get; set; }

    /// <summary>Concepteur : depot de la carte glissee sur une case (parametre : <see cref="DashboardCell"/>).</summary>
    [BindableProperty]
    public partial ICommand? DropCommand { get; set; }

    public DashboardGrid()
    {
        InitializeComponent();
        ApplyGridSize();
    }

    private static void OnGridSizeChanged(BindableObject bindable, object oldValue, object newValue)
        => ((DashboardGrid)bindable).ApplyGridSize();

    /// <summary>Les deux calques (cases, cartes) partagent les memes colonnes et lignes.</summary>
    private void ApplyGridSize()
    {
        foreach (var layer in new[] { CellsLayer, CardsLayer })
        {
            layer.ColumnDefinitions.Clear();
            for (var i = 0; i < Math.Max(1, ColumnCount); i++)
                layer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            layer.RowDefinitions.Clear();
            for (var i = 0; i < Math.Max(1, RowCount); i++)
                layer.RowDefinitions.Add(new RowDefinition(RowHeight));
        }
    }
}
