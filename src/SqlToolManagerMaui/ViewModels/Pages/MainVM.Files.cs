using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Databases.Dashboards;
using SqlToolManagerMaui.Services.Editors;
using SqlToolManagerMaui.ViewModels.Pages.Documents;
using System.Globalization;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>Partie "fichiers" : creation, ouverture, enregistrement et fermeture des onglets.</summary>
public partial class MainVM
{
    /// <summary>Compteurs des documents "Sans titre" et "Dashboard" ouverts depuis le demarrage.</summary>
    private int _untitledCount;
    private int _untitledDashboardCount;

    /// <summary>Ouvre un onglet de script vide.</summary>
    [RelayCommand]
    private void New()
    {
        _untitledCount++;
        Add(_documentFactory.CreateCodeEditor(new EditorDocument($"Sans titre {_untitledCount}", filePath: null, fileText: string.Empty)));
        _status.Clear();
    }

    /// <summary>Nouveau dashboard : une page vide, ouverte dans le concepteur, sur la connexion active.</summary>
    [RelayCommand]
    private void NewDashboard()
    {
        _untitledDashboardCount++;
        var definition = DashboardDefinition.CreateEmpty(_connection.ActiveConnection?.Name, database: null);

        // Texte « enregistre » vide : l'onglet reste modifie tant qu'il n'a pas ete enregistre.
        var document = new EditorDocument($"Dashboard {_untitledDashboardCount.ToString(CultureInfo.InvariantCulture)}", filePath: null, fileText: string.Empty, SyntaxLanguage.None)
        {
            EditorText = DashboardSerializer.Serialize(definition),
        };

        Add(_documentFactory.CreateDashboard(document, DashboardMode.Design));
    }

    [RelayCommand]
    private Task OpenAsync() => RunAsBusyAsync(async token =>
    {
        var content = await _files.OpenAsync(token);
        if (content is not null)
            await OpenContentAsync(content, token);
    });

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
                _status.Set($"{Path.GetFileName(filePath)} est introuvable : retiré des fichiers récents.", isError: true);
                return;
            }

            await OpenContentAsync(content, token);
        });
    }

    /// <summary>Ouvre le contenu dans un nouvel onglet, ou reactive celui qui l'affiche deja.</summary>
    private async Task OpenContentAsync(EditorFileContent content, CancellationToken token)
    {
        await Preferences.AddRecentFileAsync(content.FilePath, token);

        var existing = Documents.FirstOrDefault(d =>
            string.Equals(d.Document.FilePath, content.FilePath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            SelectedDocument = existing;
            _status.Set($"{existing.Document.FileName} est deja ouvert.");
            return;
        }

        var fileName = Path.GetFileName(content.FilePath);
        if (DashboardSerializer.IsDashboardFile(content.FilePath))
        {
            Add(_documentFactory.CreateDashboard(new EditorDocument(fileName, content.FilePath, content.Text, SyntaxLanguage.None)));
            return;
        }

        var language = await _syntaxConfigurations.FindLanguageAsync(content.FilePath, token) ?? SyntaxLanguage.Sql;
        Add(_documentFactory.CreateCodeEditor(new EditorDocument(fileName, content.FilePath, content.Text, language)));
    }

    [RelayCommand]
    private Task SaveAsync() => SaveDocumentAsync(SelectedDocument, forceSaveAs: false);

    [RelayCommand]
    private Task SaveAsAsync() => SaveDocumentAsync(SelectedDocument, forceSaveAs: true);

    private Task SaveDocumentAsync(DocumentVM? documentVM, bool forceSaveAs)
    {
        if (documentVM is null)
            return Task.CompletedTask;

        documentVM.PrepareSave();
        var document = documentVM.Document;

        return RunAsBusyAsync(async token =>
        {
            var text = document.EditorText;

            if (!forceSaveAs && document.HasPath)
            {
                await _files.SaveAsync(document.FilePath!, text, token);
            }
            else
            {
                var suggested = document.HasPath ? document.FileName : $"{document.FallbackName}{documentVM.DefaultExtension}";
                var path = await _files.SaveAsAsync(suggested, text, token);
                if (path is null)
                    return;

                document.FilePath = path;
                await Preferences.AddRecentFileAsync(path, token);
            }

            document.MarkSaved();
            _status.Set($"{document.FileName} enregistre.");
        });
    }

    /// <summary>Ferme un onglet ; le dernier est remplace par un document vide.</summary>
    [RelayCommand]
    private void Close(DocumentVM? document)
    {
        if (document is null)
            return;

        var index = Documents.IndexOf(document);
        if (index < 0)
            return;

        Documents.Remove(document);

        if (SelectedDocument == document)
            SelectedDocument = Documents.Count == 0 ? null : Documents[Math.Min(index, Documents.Count - 1)];

        document.Dispose();

        if (Documents.Count == 0)
            New();
    }

    /// <summary>Ferme l'onglet courant (entree de menu, sans parametre).</summary>
    [RelayCommand]
    private void CloseActive() => Close(SelectedDocument);
}
