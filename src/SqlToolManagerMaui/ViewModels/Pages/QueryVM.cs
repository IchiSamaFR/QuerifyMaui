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
public partial class QueryVM : AbstractVM
{
    private readonly IEditorFileService _files;
    private readonly IConnectionStore _connectionStore;

    /// <summary>Compteur des documents "Sans titre" ouverts depuis le demarrage.</summary>
    private int _untitledCount;

    /// <summary>Onglets ouverts, dans l'ordre d'ouverture.</summary>
    public ObservableCollection<EditorDocument> Documents { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveDocument))]
    public partial EditorDocument? ActiveDocument { get; set; }

    public bool HasActiveDocument => ActiveDocument is not null;

    /// <summary>
    /// Entree sentinelle placee en tete de <see cref="Connections"/> : la selectionner
    /// equivaut a se deconnecter. Son Id vide la distingue d'une vraie connexion.
    /// </summary>
    public static DatabaseConnectionInfo NoConnection { get; } = new()
    {
        Id = Guid.Empty,
        Name = "Aucune connexion",
    };

    /// <summary>
    /// Connexions proposees pour la future execution. Le premier element est toujours
    /// <see cref="NoConnection"/>, pour que le Picker permette de se deconnecter.
    /// </summary>
    [ObservableProperty]
    public partial List<DatabaseConnectionInfo> Connections { get; set; } = [NoConnection];

    /// <summary>Element selectionne dans le Picker, jamais null : <see cref="NoConnection"/> par defaut.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveConnection))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    public partial DatabaseConnectionInfo? SelectedConnection { get; set; } = NoConnection;

    /// <summary>Connexion reellement retenue, ou null si l'utilisateur est deconnecte.</summary>
    public DatabaseConnectionInfo? ActiveConnection
        => SelectedConnection is null || SelectedConnection.Id == Guid.Empty ? null : SelectedConnection;

    public bool IsConnected => ActiveConnection is not null;

    /// <summary>Message affiche dans la barre d'etat (enregistrement, erreur d'ouverture...).</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsStatusError { get; set; }

    // ---------- Options d'edition, liees directement au CodeEditor ----------

    public EditorDocumentConfiguration DocumentConfiguration { get; } = new()
    {
        IsLineDiplayed = true,
        IsWordWrapped = false,
        IsTabIndentation = false,
        IdentationSize = 4
    };

    // ---------- Barre d'etat ----------

    [ObservableProperty]
    public partial int CaretLine { get; set; } = 1;

    [ObservableProperty]
    public partial int CaretColumn { get; set; } = 1;

    public QueryVM(IEditorFileService files, IConnectionStore connectionStore)
    {
        _files = files;
        _connectionStore = connectionStore;
    }

    protected override async Task LoadDatasBusyAsync()
    {
        var stored = await _connectionStore.GetAllAsync(LifetimeToken);

        // La sentinelle "Aucune connexion" reste en tete : c'est l'option de deconnexion.
        Connections = [NoConnection, .. stored];

        // Une connexion supprimee entre-temps ne doit pas rester selectionnee.
        if (ActiveConnection is not null && !stored.Any(c => c.Id == ActiveConnection.Id))
            SelectedConnection = NoConnection;

        if (Documents.Count == 0)
            New();
    }

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
        => DocumentConfiguration.IsLineDiplayed = !DocumentConfiguration.IsLineDiplayed;

    [RelayCommand]
    private void ToggleWordWrap()
        => DocumentConfiguration.IsWordWrapped = !DocumentConfiguration.IsWordWrapped;

    [RelayCommand]
    private void ToggleUseTabs()
        => DocumentConfiguration.IsTabIndentation = !DocumentConfiguration.IsTabIndentation;

    /// <summary>Ferme l'application (entree Fichier > Quitter).</summary>
    [RelayCommand]
    private static void Quit() => Application.Current?.Quit();

    // ---------- Connexion ----------

    /// <summary>Repasse le Picker sur "Aucune connexion".</summary>
    [RelayCommand(CanExecute = nameof(IsConnected))]
    private void Disconnect()
    {
        SelectedConnection = NoConnection;
    }

    partial void OnSelectedConnectionChanged(DatabaseConnectionInfo? value)
    {
        SetStatus(ActiveConnection is null
            ? "Deconnecte."
            : $"Connexion active : {ActiveConnection.Name}.");
    }

    // ---------- Execution (a venir) ----------

    /// <summary>
    /// Volontairement inactive : l'UI est en place, l'execution sera branchee
    /// sur la connexion selectionnee dans une prochaine etape.
    /// </summary>
    [RelayCommand]
    private void ExecuteQuery()
    {
        SetStatus("L'execution des requetes n'est pas encore disponible.");
    }

    // ---------- Appels depuis la vue ----------

    /// <summary>Appele par la page a chaque edition du contenu.</summary>
    public void MarkActiveDocumentDirty()
    {
        if (ActiveDocument is not null)
            ActiveDocument.IsDirty = true;
    }

    /// <summary>Appele par la page a chaque deplacement du curseur.</summary>
    public void UpdateCaret(int line, int column)
    {
        CaretLine = line;
        CaretColumn = column;
    }

    public void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
