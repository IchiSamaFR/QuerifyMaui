using CodeEdit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Models.Editors;

/// <summary>
/// Un onglet de l'editeur : le document CodeEdit, son chemin sur disque (null tant
/// qu'il n'a jamais ete enregistre) et son etat modifie.
/// Le texte n'est jamais duplique ici : <see cref="CodeDocument.Text"/> fait foi.
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

    /// <summary>Vrai des que le contenu differe de la derniere version enregistree.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial bool IsDirty { get; set; }

    /// <summary>Document passe a <c>CodeEditor.Document</c>.</summary>
    [ObservableProperty]
    public partial CodeDocument Document { get; set; }

    /// <summary>Libelle utilise tant que le document n'a pas de chemin (ex. "Sans titre 1").</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyPropertyChangedFor(nameof(FileName))]
    public partial string FallbackName { get; set; }

    public bool HasPath => !string.IsNullOrEmpty(FilePath);

    public string FileName => HasPath ? Path.GetFileName(FilePath!) : FallbackName;

    /// <summary>Nom affiche dans l'onglet, suffixe d'une pastille quand le document est modifie.</summary>
    public string Title => IsDirty ? $"{FileName} •" : FileName;

    /// <summary>Chemin affiche dans la barre d'etat.</summary>
    public string PathLabel => HasPath ? FilePath! : "Non enregistre";

    public EditorDocument(string fallbackName, string? filePath, string text)
    {
        FallbackName = fallbackName;
        FilePath = filePath;
        Document = CodeDocument.FromPlainText(text);
    }

    /// <summary>Remplace le contenu (rechargement depuis le disque) et repart d'un etat propre.</summary>
    public void Reset(string text, string? filePath)
    {
        Document = CodeDocument.FromPlainText(text);
        FilePath = filePath;
        IsDirty = false;
    }
}
