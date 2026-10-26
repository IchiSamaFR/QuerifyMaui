using CommunityToolkit.Mvvm.ComponentModel;

namespace Querify.Core.Models.Settings;

/// <summary>Options d'affichage de l'editeur, partagees par tous les onglets.</summary>
public partial class EditorPreferences : ObservableObject
{
    [ObservableProperty]
    public partial bool IsLineDisplayed { get; set; } = true;

    [ObservableProperty]
    public partial bool IsWordWrapped { get; set; }

    [ObservableProperty]
    public partial bool IsTabIndentation { get; set; }

    [ObservableProperty]
    public partial int IndentationSize { get; set; } = 4;

    /// <summary>Un nouveau script s'ouvre sur le modele de son langage (prise en main).</summary>
    [ObservableProperty]
    public partial bool InsertTemplateOnNew { get; set; } = true;
}
