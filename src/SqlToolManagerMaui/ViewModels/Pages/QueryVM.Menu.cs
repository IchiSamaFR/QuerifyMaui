using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Models.Databases;
using SqlToolManagerMaui.Models.Editors;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>
/// Editeur de texte multi-onglets (facon Notepad) pose sur CodeEdit.Maui.
/// Chaque onglet est un <see cref="EditorDocument"/> ; la page n'affiche qu'un seul
/// <c>CodeEditor</c>, dont la propriete Document suit <see cref="ActiveDocument"/>.
/// Le choix de connexion et la commande Executer sont deja cables mais inactifs :
/// l'execution des requetes viendra dans une etape ulterieure.
/// </summary>
public partial class QueryVM
{
    // ---------- Commandes fichier ----------

    /// <summary>Ouvre un onglet vide et l'active.</summary>
    [RelayCommand]
    private void New()
    {
        _untitledCount++;
        var document = new EditorDocument($"Sans titre {_untitledCount}", filePath: null, text: string.Empty);

        Documents.Add(document);
        ActiveDocument = document;
        SetStatus(string.Empty);
    }

    [RelayCommand]
    private Task OpenAsync() => RunAsBusyAsync(async token =>
    {
        var content = await _files.OpenAsync(token);
        if (content is null)
            return;

        // Un fichier deja ouvert est reactive plutot que duplique.
        var existing = Documents.FirstOrDefault(d =>
            string.Equals(d.FilePath, content.FilePath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            ActiveDocument = existing;
            SetStatus($"{existing.FileName} est deja ouvert.");
            return;
        }

        var document = new EditorDocument(Path.GetFileName(content.FilePath), content.FilePath, content.Text);
        Documents.Add(document);
        ActiveDocument = document;
        SetStatus($"{document.FileName} ouvert.");
    });

    [RelayCommand]
    private Task SaveAsync() => SaveDocumentAsync(ActiveDocument, forceSaveAs: false);

    [RelayCommand]
    private Task SaveAsAsync() => SaveDocumentAsync(ActiveDocument, forceSaveAs: true);

    private Task SaveDocumentAsync(EditorDocument? document, bool forceSaveAs)
    {
        if (document is null)
            return Task.CompletedTask;

        return RunAsBusyAsync(async token =>
        {
            var text = document.Document.Text;

            if (!forceSaveAs && document.HasPath)
            {
                await _files.SaveAsync(document.FilePath!, text, token);
            }
            else
            {
                var suggested = document.HasPath ? document.FileName : $"{document.FallbackName}.sql";
                var path = await _files.SaveAsAsync(suggested, text, token);
                if (path is null)
                    return;

                document.FilePath = path;
            }

            document.IsDirty = false;
            SetStatus($"{document.FileName} enregistre.");
        });
    }

    /// <summary>Ferme un onglet. Un document modifie doit etre confirme par la page.</summary>
    [RelayCommand]
    private void Close(EditorDocument? document)
    {
        if (document is null)
            return;

        var index = Documents.IndexOf(document);
        if (index < 0)
            return;

        Documents.Remove(document);

        if (ActiveDocument == document)
            ActiveDocument = Documents.Count == 0 ? null : Documents[Math.Min(index, Documents.Count - 1)];

        if (Documents.Count == 0)
            New();
    }

    /// <summary>Active l'onglet clique.</summary>
    [RelayCommand]
    private void Activate(EditorDocument? document)
    {
        if (document is not null)
            ActiveDocument = document;
    }

    /// <summary>Ferme l'onglet courant (entree de menu, sans parametre).</summary>
    [RelayCommand]
    private void CloseActive() => Close(ActiveDocument);

    // ---------- Menu Affichage ----------

    [RelayCommand]
    private void ToggleLineNumbers()
        => EditorConfiguration.IsLineDisplayed = !EditorConfiguration.IsLineDisplayed;

    [RelayCommand]
    private void ToggleWordWrap()
        => EditorConfiguration.IsWordWrapped = !EditorConfiguration.IsWordWrapped;

    [RelayCommand]
    private void ToggleUseTabs()
        => EditorConfiguration.IsTabIndentation = !EditorConfiguration.IsTabIndentation;

}
