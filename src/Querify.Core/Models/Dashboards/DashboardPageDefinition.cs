using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace Querify.Core.Models.Dashboards;

/// <summary>Page (sous-partie) du dashboard : une grille de cartes.</summary>
public sealed partial class DashboardPageDefinition : ObservableObject
{
    /// <summary>Identifiant (GUID) utilise par la navigation ; jamais modifie.</summary>
    public required string Id { get; init; }

    /// <summary>Observable : renommee depuis son onglet dans le concepteur.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    public partial string Title { get; set; } = string.Empty;

    // set et non init : le generateur System.Text.Json remet a null une propriete init absente du JSON.
    public List<DashboardCardDefinition> Cards { get; set; } = [];

    /// <summary>Libelle affiche (fil d'Ariane, choix de page) quand le titre est vide.</summary>
    public const string UntitledTitle = "(sans titre)";

    /// <summary>Titre affiche : <see cref="UntitledTitle"/> si aucun n'est renseigne.</summary>
    [JsonIgnore]
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? UntitledTitle : Title;

    public static string NewId() => Guid.NewGuid().ToString("N");
}
