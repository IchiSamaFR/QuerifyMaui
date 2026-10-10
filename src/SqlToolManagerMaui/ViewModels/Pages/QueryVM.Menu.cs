using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Editors;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Databases.Dashboards;
using SqlToolManagerMaui.Services.Editors;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>
/// Editeur de texte multi-onglets (facon Notepad) pose sur CodeEdit.Maui.
/// Chaque onglet est un <see cref="EditorDocument"/> ; la page n'affiche qu'un seul
/// <c>CodeEditor</c>, dont la propriete Document suit <see cref="ActiveDocument"/>.
/// Partie "menus" : commandes fichier, onglets et affichage.
/// </summary>
public partial class QueryVM
{
    // ---------- Commandes fichier ----------

    /// <summary>Ouvre un onglet vide et l'active.</summary>
    [RelayCommand]
    private void New()
    {
        _untitledCount++;
        var document = new EditorDocument($"Sans titre {_untitledCount}", filePath: null, fileText: string.Empty);

        Documents.Add(document);
        ActiveDocument = document;
        Status.Clear();
    }

    [RelayCommand]
    private Task OpenAsync() => RunAsBusyAsync(async token =>
    {
        var content = await _files.OpenAsync(token);
        if (content is not null)
            await OpenContentAsync(content, token);
    });

    /// <summary>Ouvre le contenu dans un nouvel onglet, ou reactive celui qui l'affiche deja.</summary>
    private async Task OpenContentAsync(EditorFileContent content, CancellationToken token)
    {
        await Preferences.AddRecentFileAsync(content.FilePath, token);

        // Un fichier deja ouvert est reactive plutot que duplique.
        var existing = Documents.FirstOrDefault(d =>
            string.Equals(d.FilePath, content.FilePath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            ActiveDocument = existing;
            Status.Set($"{existing.FileName} est deja ouvert.");
            return;
        }

        if (DashboardSerializer.IsDashboardFile(content.FilePath))
        {
            Dashboard.Open(new EditorDocument(Path.GetFileName(content.FilePath), content.FilePath, content.Text, SyntaxLanguage.None, DocumentKind.Dashboard));
            return;
        }

        var language = await _syntaxConfigurations.FindLanguageAsync(content.FilePath, token) ?? SyntaxLanguage.Sql;
        var document = new EditorDocument(Path.GetFileName(content.FilePath), content.FilePath, content.Text, language);
        Documents.Add(document);
        ActiveDocument = document;
        Status.Set($"{document.FileName} ouvert.");
    }

    /// <summary>Rouvre un fichier recent ; un fichier disparu est retire de la liste.</summary>
    [RelayCommand]
    private Task OpenRecentAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.CompletedTask;

        return RunAsBusyAsync(async token =>
        {
            var content = await _files.ReadAsync(filePath, token);
            if (content is null)
            {
                await Preferences.RemoveRecentFileAsync(filePath, token);
                Status.Set($"{Path.GetFileName(filePath)} est introuvable : retiré des fichiers récents.", isError: true);
                return;
            }

            await OpenContentAsync(content, token);
        });
    }

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
            var text = document.EditorText;

            if (!forceSaveAs && document.HasPath)
            {
                await _files.SaveAsync(document.FilePath!, text, token);
            }
            else
            {
                var suggested = document.HasPath ? document.FileName : $"{document.FallbackName}{DefaultExtension(document)}";
                var path = await _files.SaveAsAsync(suggested, text, token);
                if (path is null)
                    return;

                document.FilePath = path;
                await Preferences.AddRecentFileAsync(path, token);
            }

            document.MarkSaved();
            Status.Set($"{document.FileName} enregistre.");
        });
    }

    /// <summary>.schx pour un dashboard, sinon la premiere extension declaree par le .xshd du langage.</summary>
    private string DefaultExtension(EditorDocument document)
        => document.IsDashboard
            ? DashboardSerializer.FILE_EXTENSION
            : _syntaxByLanguage.TryGetValue(document.Language, out var configuration) && configuration.Definition.Extensions.Count > 0
                ? configuration.Definition.Extensions[0]
                : ".sql";

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

    /// <summary>Change le langage de l'onglet actif ; le texte n'est pas touche.</summary>
    [RelayCommand]
    private void SetLanguage(SyntaxLanguage language)
    {
        if (ActiveDocument is not { } document || document.Language == language)
            return;

        document.Language = language;
        OnPropertyChanged(nameof(SyntaxConfiguration));
        Status.Set($"Langage de l'onglet : {SyntaxConfiguration?.Definition.Name ?? language.ToString()}.");
    }
}
