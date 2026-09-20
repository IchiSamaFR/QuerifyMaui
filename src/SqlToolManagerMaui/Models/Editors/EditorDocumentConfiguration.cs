using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Models.Editors;

/// <summary>
/// Options d'affichage de l'editeur, partagees par tous les onglets.
/// Les libelles *Label sont destines au menu : MenuFlyoutItem n'a pas d'etat "coche",
/// c'est donc le texte qui porte l'action a venir.
/// </summary>
public partial class EditorDocumentConfiguration : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineDisplayLabel))]
    public partial bool IsLineDiplayed { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WordWrapLabel))]
    public partial bool IsWordWrapped { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IndentationLabel))]
    public partial bool IsTabIndentation { get; set; }

    [ObservableProperty]
    public partial int IdentationSize { get; set; }

    public string LineDisplayLabel => IsLineDiplayed
        ? "Masquer les numéros de ligne"
        : "Afficher les numéros de ligne";

    public string WordWrapLabel => IsWordWrapped
        ? "Désactiver le retour à la ligne"
        : "Activer le retour à la ligne";

    public string IndentationLabel => IsTabIndentation
        ? "Indenter avec des espaces"
        : "Indenter avec des tabulations";
}
