using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Core.Models.Editors;

/// <summary>
/// Options d'affichage de l'editeur, partagees par tous les onglets.
/// Les libelles du menu sont portes par ViewMenuBarItem : ce modele n'expose que l'etat.
/// </summary>
public partial class EditorConfiguration : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLineDisplayed { get; set; }

    [ObservableProperty]
    public partial bool IsWordWrapped { get; set; }

    [ObservableProperty]
    public partial bool IsTabIndentation { get; set; }

    [ObservableProperty]
    public partial int IndentationSize { get; set; }
}
