using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Models.Editors;

/// <summary>
/// Un onglet de l'editeur : le texte enregistre, le texte en cours d'edition et le chemin
/// sur disque (null tant que le document n'a jamais ete enregistre). L'ecart entre les deux
/// textes fait l'etat modifie.
/// </summary>
public sealed partial class EditorDocument : ObservableObject
{
    /// <summary>Chemin complet du fichier, ou null pour un document jamais enregistre.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(FileName))]
    [NotifyPropertyChangedFor(nameof(HasPath))]
    [NotifyPropertyChangedFor(nameof(PathLabel))]
    public partial string? FilePath { get; set; }

    /// <summary>Contenu de la derniere version enregistree.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial string FileText { get; set; }

    /// <summary>Contenu courant de l'editeur.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial string EditorText { get; set; }

    /// <summary>Libelle utilise tant que le document n'a pas de chemin (ex. "Sans titre 1").</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(FileName))]
    public partial string FallbackName { get; set; }

    /// <summary>Vrai des que le contenu differe de la derniere version enregistree.</summary>
    public bool IsDirty => !string.Equals(FileText, EditorText, StringComparison.Ordinal);

    public bool HasPath => !string.IsNullOrEmpty(FilePath);

    public string FileName => HasPath ? Path.GetFileName(FilePath!) : FallbackName;

    /// <summary>Nom affiche dans l'onglet, suffixe d'une pastille quand le document est modifie.</summary>
    public string Title => IsDirty ? $"{FileName} •" : FileName;

    /// <summary>Chemin affiche dans la barre d'etat.</summary>
    public string PathLabel => HasPath ? FilePath! : "Non enregistre";

    public EditorDocument(string fallbackName, string? filePath, string fileText)
    {
        FallbackName = fallbackName;
        FileText = Normalize(fileText);
        EditorText = FileText;
        FilePath = filePath;
    }

    /// <summary>Remplace le contenu (rechargement depuis le disque) et repart d'un etat propre.</summary>
    public void Reset(string fileText, string? filePath)
    {
        FileText = Normalize(fileText);
        EditorText = FileText;
        FilePath = filePath;
    }

    /// <summary>Marque le contenu courant comme enregistre.</summary>
    public void MarkSaved() => FileText = EditorText;

    /// <summary>
    /// L'editeur normalise les fins de ligne en LF : on aligne le texte disque dessus,
    /// sinon un fichier CRLF paraitrait modifie des son ouverture.
    /// </summary>
    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
