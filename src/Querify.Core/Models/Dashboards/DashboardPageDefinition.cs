using CommunityToolkit.Mvvm.ComponentModel;

namespace Querify.Core.Models.Dashboards;

/// <summary>Page (sous-partie) du dashboard : une grille de cartes.</summary>
public sealed class DashboardPageDefinition : ObservableObject
{
    private string _id = string.Empty;
    private string _title = string.Empty;

    /// <summary>Cle utilisee par la navigation (unique dans le fichier) ; modifiable depuis les options de la page.</summary>
    public required string Id
    {
        get => _id;
        set => SetProperty(ref _id, value ?? string.Empty);
    }

    /// <summary>Observable : renommee depuis son onglet dans le concepteur.</summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value ?? string.Empty);
    }

    // set et non init : le generateur System.Text.Json remet a null une propriete init absente du JSON.
    public List<DashboardCardDefinition> Cards { get; set; } = [];
}
