using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Models.Editors;

/// <summary>
/// Etat de la selection de l'editeur : position du curseur et texte selectionne.
/// Alimente par le CodeEditor a chaque changement de selection, lu par l'hote
/// (barre d'etat, execution de la selection...). Chaque document porte la sienne :
/// le CodeEditor la restaure a partir de <see cref="Anchor"/> / <see cref="Active"/>.
/// </summary>
public partial class EditorSelection : ObservableObject
{
    /// <summary>Ligne du curseur (base 1).</summary>
    [ObservableProperty]
    public partial int CaretLine { get; set; } = 1;

    /// <summary>Colonne du curseur (base 1, unites UTF-16).</summary>
    [ObservableProperty]
    public partial int CaretColumn { get; set; } = 1;

    /// <summary>Extremite fixe de la selection (offset UTF-16, base 0).</summary>
    [ObservableProperty]
    public partial int Anchor { get; set; }

    /// <summary>Extremite mobile de la selection, ou se trouve le curseur (offset UTF-16, base 0).</summary>
    [ObservableProperty]
    public partial int Active { get; set; }

    /// <summary>Texte actuellement selectionne, vide si aucune selection.</summary>
    [ObservableProperty]
    public partial string SelectedText { get; set; } = string.Empty;
}
