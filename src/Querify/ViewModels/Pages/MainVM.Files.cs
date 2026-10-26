using CommunityToolkit.Mvvm.Input;
using Querify.Core.Enums.Dashboards;
using Querify.Core.Enums.Editors;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Dashboards;
using Querify.Core.Models.Editors;
using Querify.Core.Serialization;
using Querify.Services.Editors;
using System.Globalization;

namespace Querify.ViewModels.Pages;

/// <summary>Partie "fichiers" : creation, ouverture, enregistrement et fermeture des onglets.</summary>
public partial class MainVM
{
    private const int MaxRecentFiles = 10;

    /// <summary>Compteurs des documents "Sans titre" et "Dashboard" ouverts depuis le demarrage.</summary>
    private int _untitledCount;
    private int _untitledDashboardCount;

    /// <summary>Ouvre un onglet de script vide.</summary>
    [RelayCommand]
    private void New()
    {
        _untitledCount++;
        Add(new CodeEditorSession(new EditorDocument($"Sans titre {_untitledCount}", filePath: null, fileText: string.Empty)));
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

        Add(new DashboardSession(document) { Mode = DashboardMode.Design });
        _status.Set($"{document.FileName} ouvert.");
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
                RemoveRecentFile(filePath);
                _status.Set($"{Path.GetFileName(filePath)} est introuvable : retiré des fichiers récents.", isError: true);
                return;
            }

            await OpenContentAsync(content, token);
        });
    }

    /// <summary>Ouvre le contenu dans un nouvel onglet, ou reactive celui qui l'affiche deja.</summary>
    private async Task OpenContentAsync(EditorFileContent content, CancellationToken token)
    {
        AddRecentFile(content.FilePath);

        var existing = Sessions.FirstOrDefault(s =>
            string.Equals(s.Document.FilePath, content.FilePath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            SelectedSession = existing;
            _status.Set($"{existing.Document.FileName} est deja ouvert.");
            return;
        }

        var fileName = Path.GetFileName(content.FilePath);
        if (DashboardSerializer.IsDashboardFile(content.FilePath))
        {
            _status.Set($"{fileName} ouvert.");
            Add(new DashboardSession(new EditorDocument(fileName, content.FilePath, content.Text, SyntaxLanguage.None)));
            return;
        }

        var language = await _syntaxConfigurations.FindLanguageAsync(content.FilePath, token) ?? SyntaxLanguage.Sql;
        Add(new CodeEditorSession(new EditorDocument(fileName, content.FilePath, content.Text, language)));
        _status.Set($"{fileName} ouvert.");
    }

    [RelayCommand]
    private async Task SaveAsync() => await TrySaveSessionAsync(SelectedSession, forceSaveAs: false);

    [RelayCommand]
    private async Task SaveAsAsync() => await TrySaveSessionAsync(SelectedSession, forceSaveAs: true);

    /// <summary>Faux si l'enregistrement a echoue (erreur publiee par RunAsBusyAsync) ou si « Enregistrer sous » a ete annule.</summary>
    private async Task<bool> TrySaveSessionAsync(AbstractSession? session, bool forceSaveAs)
    {
        if (session is null)
            return false;

        // Dernieres modifications du concepteur pas encore reecrites dans le texte.
        _dashboard.FlushPendingWrites();
        var document = session.Document;

        return await RunAsBusyAsync(async token =>
        {
            var text = document.EditorText;

            if (!forceSaveAs && document.HasPath)
            {
                await _files.SaveAsync(document.FilePath!, text, token);
            }
            else
            {
                var suggested = document.HasPath ? document.FileName : $"{document.FallbackName}{await DefaultExtensionAsync(session, token)}";
                var path = await _files.SaveAsAsync(suggested, text, token);
                if (path is null)
                    return false;

                document.FilePath = path;
                AddRecentFile(path);
                NotifyTabPathChanged();
            }

            document.MarkSaved();
            _status.Set($"{document.FileName} enregistre.");
            return true;
        });
    }

    /// <summary>.schx pour un dashboard, sinon la premiere extension declaree par le .xshd du langage.</summary>
    private async Task<string> DefaultExtensionAsync(AbstractSession session, CancellationToken token)
    {
        if (session is DashboardSession)
            return DashboardSerializer.FILE_EXTENSION;

        var configuration = await _syntaxConfigurations.GetAsync(session.Document.Language, token);
        return configuration is { Definition.Extensions: [var extension, ..] } ? extension : ".sql";
    }

    /// <summary>Ferme un onglet, apres confirmation s'il est modifie ; le dernier est remplace par un document vide.</summary>
    [RelayCommand]
    private async Task CloseAsync(AbstractSession? session)
    {
        if (session is null || !Sessions.Contains(session))
            return;

        // Remove() selectionne deja l'onglet voisin de gauche si l'onglet ferme etait actif.
        if (await ConfirmDiscardAsync(session))
            Remove(session);
    }

    /// <summary>
    /// Onglet a activer apres la fermeture de celui situe a <paramref name="closedIndex"/> :
    /// le plus proche a gauche, sinon (premier onglet) le nouveau premier. Appeler apres le retrait.
    /// </summary>
    private AbstractSession? GetNeighborSession(int closedIndex)
    {
        if (Sessions.Count == 0)
            return null;

        return Sessions[Math.Clamp(closedIndex - 1, 0, Sessions.Count - 1)];
    }

    private void Remove(AbstractSession session)
    {
        var index = Sessions.IndexOf(session);
        if (index < 0)
            return;

        var wasSelected = SelectedSession == session;

        Sessions.Remove(session);

        if (wasSelected)
            SelectedSession = GetNeighborSession(index);

        session.Dispose();

        if (Sessions.Count == 0)
            New();
    }

    /// <summary>Vrai si l'onglet peut etre ferme : non modifie, enregistre, ou modifications abandonnees.</summary>
    private async Task<bool> ConfirmDiscardAsync(AbstractSession session)
    {
        // Dernieres modifications du concepteur : elles comptent dans IsDirty.
        _dashboard.FlushPendingWrites();
        if (!session.Document.IsDirty)
            return true;

        // L'utilisateur voit le document dont il est question.
        SelectedSession = session;

        return await _dialogs.AskSaveChangesAsync(session.Document.FileName) switch
        {
            SaveChangesChoice.Save => await TrySaveSessionAsync(session, forceSaveAs: false),
            SaveChangesChoice.Discard => true,
            _ => false,
        };
    }

    /// <summary>Ferme l'onglet courant (entree de menu, sans parametre).</summary>
    [RelayCommand]
    private Task CloseActiveAsync() => CloseAsync(SelectedSession);

    // ---------- Menu contextuel d'un onglet (parametre : la session de l'onglet) ----------

    [RelayCommand]
    private Task CloseOtherTabsAsync(AbstractSession? session)
        => CloseManyAsync(Sessions.Where(s => !ReferenceEquals(s, session)).ToList());

    [RelayCommand]
    private Task CloseTabsToRightAsync(AbstractSession? session)
    {
        var index = session is null ? -1 : Sessions.IndexOf(session);
        return index < 0 ? Task.CompletedTask : CloseManyAsync(Sessions.Skip(index + 1).ToList());
    }

    [RelayCommand]
    private Task CloseAllTabsAsync() => CloseManyAsync(Sessions.ToList());

    /// <summary>Un par un, chaque onglet modifie etant confirme : « Annuler » arrete la fermeture des suivants.</summary>
    private async Task CloseManyAsync(IReadOnlyList<AbstractSession> sessions)
    {
        foreach (var session in sessions)
        {
            if (!Sessions.Contains(session))
                continue;

            if (!await ConfirmDiscardAsync(session))
                return;

            Remove(session);
        }
    }

    [RelayCommand]
    private async Task SaveTabAsync(AbstractSession? session) => await TrySaveSessionAsync(session, forceSaveAs: false);

    [RelayCommand]
    private async Task SaveTabAsAsync(AbstractSession? session) => await TrySaveSessionAsync(session, forceSaveAs: true);

    /// <summary>Fichier enregistre : renomme sur le disque ; document sans titre : change son nom d'onglet.</summary>
    [RelayCommand]
    private async Task RenameTabAsync(AbstractSession? session)
    {
        if (session is null)
            return;

        var document = session.Document;
        var name = await _dialogs.PromptAsync(
            "Renommer",
            document.HasPath ? $"Nouveau nom du fichier « {document.FileName} » :" : "Nouveau nom de l'onglet :",
            document.FileName,
            "Renommer",
            value => ValidateName(document, value));

        if (name is null || name == document.FileName)
            return;

        if (!document.HasPath)
        {
            document.FallbackName = name;
            return;
        }

        // Sans extension saisie, celle du fichier est conservee.
        var fileName = Path.HasExtension(name) ? name : name + Path.GetExtension(document.FilePath);
        var oldPath = document.FilePath!;

        await RunAsBusyAsync(async token =>
        {
            var newPath = await _files.RenameAsync(oldPath, fileName, token);
            document.FilePath = newPath;

            if (Preferences.RecentFiles.Any(p => IsSamePath(p, oldPath)))
                Preferences.RecentFiles = [.. Preferences.RecentFiles.Select(p => IsSamePath(p, oldPath) ? newPath : p)];

            if (Preferences.TrustedDashboards.Any(p => IsSamePath(p, oldPath)))
                Preferences.TrustedDashboards = [.. Preferences.TrustedDashboards.Select(p => IsSamePath(p, oldPath) ? newPath : p)];

            _status.Set($"{Path.GetFileName(oldPath)} renommé en {fileName}.");
        });
    }

    private static string? ValidateName(EditorDocument document, string name)
    {
        if (name.Length == 0)
            return "Le nom est obligatoire.";

        if (document.HasPath && name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "Le nom contient des caractères interdits dans un nom de fichier.";

        return null;
    }

    [RelayCommand(CanExecute = nameof(CanRevealTab))]
    private void RevealTab(AbstractSession? session)
    {
        if (session?.Document.FilePath is not { } path)
            return;

        if (!_files.RevealInExplorer(path))
            _status.Set($"{Path.GetFileName(path)} est introuvable sur le disque.", isError: true);
    }

    private static bool CanRevealTab(AbstractSession? session) => session?.Document.HasPath == true;

    [RelayCommand(CanExecute = nameof(CanRevealTab))]
    private async Task CopyTabPathAsync(AbstractSession? session)
    {
        if (session?.Document.FilePath is not { } path)
            return;

        await Clipboard.Default.SetTextAsync(path);
        _status.Set("Chemin copié dans le presse-papiers.");
    }

    /// <summary>Un document qui recoit un chemin (Enregistrer sous, renommage) rend ses entrees de menu disponibles.</summary>
    private void NotifyTabPathChanged()
    {
        RevealTabCommand.NotifyCanExecuteChanged();
        CopyTabPathCommand.NotifyCanExecuteChanged();
    }

    // ---------- Fichiers recents ----------

    [RelayCommand]
    private void ClearRecentFiles() => Preferences.RecentFiles = [];

    /// <summary>Place le fichier en tete de liste (sans doublon) et tronque au-dela de <see cref="MaxRecentFiles"/>.</summary>
    private void AddRecentFile(string filePath)
        => Preferences.RecentFiles = [filePath, .. Preferences.RecentFiles.Where(p => !IsSamePath(p, filePath)).Take(MaxRecentFiles - 1)];

    private void RemoveRecentFile(string filePath)
    {
        if (Preferences.RecentFiles.Any(p => IsSamePath(p, filePath)))
            Preferences.RecentFiles = [.. Preferences.RecentFiles.Where(p => !IsSamePath(p, filePath))];
    }

    private static bool IsSamePath(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
