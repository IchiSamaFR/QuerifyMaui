using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;

namespace SqlToolManagerMaui.Core.Models.Editors;

/// <summary>
/// Fichier d'un onglet : le texte enregistre, le texte en cours d'edition et le chemin
/// sur disque (null tant que le document n'a jamais ete enregistre). L'ecart entre les deux
/// textes fait l'etat modifie. L'etat d'affichage (resultats, dashboard) appartient au ViewModel de l'onglet.
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

    /// <summary>Langage de l'onglet : coloration, autocompletion et moteur d'execution en dependent.</summary>
    [ObservableProperty]
    public partial SyntaxLanguage Language { get; set; }

    /// <summary>Selection propre a l'onglet, restauree par l'editeur quand l'onglet redevient actif.</summary>
    [ObservableProperty]
    public partial EditorSelection Selection { get; set; } = new();

    /// <summary>Vrai des que le contenu differe de la derniere version enregistree.</summary>
    public bool IsDirty => !string.Equals(FileText, EditorText, StringComparison.Ordinal);

    public bool HasPath => !string.IsNullOrEmpty(FilePath);

    public string FileName => HasPath ? Path.GetFileName(FilePath!) : FallbackName;

    /// <summary>Nom affiche dans l'onglet, suffixe d'une pastille quand le document est modifie.</summary>
    public string Title => IsDirty ? $"{FileName} •" : FileName;

    /// <summary>Chemin affiche dans la barre d'etat.</summary>
    public string PathLabel => HasPath ? FilePath! : "Non enregistre";

    public EditorDocument(string fallbackName, string? filePath, string fileText, SyntaxLanguage language = SyntaxLanguage.Sql)
    {
        Language = language;
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

    /// <summary>Insere un bloc au curseur (a la place de la selection), sur sa propre ligne, curseur apres le bloc.</summary>
    public void InsertBlock(string block)
    {
        var text = EditorText ?? string.Empty;
        var newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : text.Contains('\r') ? "\r" : Environment.NewLine;

        var start = Math.Clamp(Math.Min(Selection.Anchor, Selection.Active), 0, text.Length);
        var end = Math.Clamp(Math.Max(Selection.Anchor, Selection.Active), 0, text.Length);

        // Curseur en milieu de ligne : le bloc part sur une nouvelle ligne.
        var prefix = start > 0 && text[start - 1] is not ('\n' or '\r') ? newLine : string.Empty;
        var inserted = prefix + block.Replace("\n", newLine, StringComparison.Ordinal) + newLine;

        // Selection d'abord : l'editeur la restaure apres le changement de texte.
        Selection.Anchor = Selection.Active = start + inserted.Length;
        EditorText = text[..start] + inserted + text[end..];
    }

    /// <summary>
    /// L'editeur normalise les fins de ligne en LF : on aligne le texte disque dessus,
    /// sinon un fichier CRLF paraitrait modifie des son ouverture.
    /// </summary>
    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
