using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Models.Editors;

/// <summary>
/// Options d'affichage de l'editeur, partagees par tous les onglets.
/// Les libelles du menu sont portes par ViewMenuBarItem : ce modele n'expose que l'etat.
/// </summary>
public partial class EditorDocumentConfiguration : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLineDiplayed { get; set; }

    [ObservableProperty]
    public partial bool IsWordWrapped { get; set; }

    [ObservableProperty]
    public partial bool IsTabIndentation { get; set; }

    [ObservableProperty]
    public partial int IdentationSize { get; set; }
}
