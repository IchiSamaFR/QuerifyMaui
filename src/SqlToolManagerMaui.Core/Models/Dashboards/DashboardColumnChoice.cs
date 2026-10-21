using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Core.Models.Dashboards;

/// <summary>Colonne proposee au concepteur pour les valeurs d'une carte (selection multiple).</summary>
public sealed partial class DashboardColumnChoice : ObservableObject
{
    public string Name { get; }

    public DashboardColumnChoice(string name)
    {
        Name = name;
    }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Rang parmi les colonnes choisies (base 1) : l'ordre des series d'un graphique ; 0 si non choisie.</summary>
    [ObservableProperty]
    public partial int Order { get; set; }

    /// <summary>Faux pour une colonne choisie auparavant mais absente du dernier resultat.</summary>
    [ObservableProperty]
    public partial bool IsAvailable { get; set; } = true;
}
