using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.Models.Databases;
using SqlToolManagerMaui.Models.Editors;
using SqlToolManagerMaui.Models.Editors.Highlighting;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Editors;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.UI.ViewModels;

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
    private readonly ISyntaxConfigurationStore _syntaxConfigurations;

    /// <summary>Compteur des documents "Sans titre" ouverts depuis le demarrage.</summary>
    private int _untitledCount;

    /// <summary>Onglets ouverts, dans l'ordre d'ouverture.</summary>
    public ObservableCollection<EditorDocument> Documents { get; } = new();

    [ObservableProperty]
    public partial EditorDocument? ActiveDocument { get; set; }

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

    public EditorConfiguration EditorConfiguration { get; } = new()
    {
        IsLineDisplayed = true,
        IsWordWrapped = false,
        IsTabIndentation = false,
        IndentationSize = 4
    };

    // ---------- Barre d'etat ----------

    [ObservableProperty]
    public partial int CaretLine { get; set; } = 1;

    [ObservableProperty]
    public partial int CaretColumn { get; set; } = 1;

    /// <summary>Langage colore par l'editeur : pour l'instant toujours du T-SQL.</summary>
    [ObservableProperty]
    public partial SyntaxConfiguration? SyntaxConfiguration { get; set; }

    public QueryVM(IEditorFileService files, IConnectionStore connectionStore, ISyntaxConfigurationStore syntaxConfigurations)
    {
        _files = files;
        _connectionStore = connectionStore;
        _syntaxConfigurations = syntaxConfigurations;

        // Ajout / modification / suppression depuis l'explorateur : le Picker suit.
        WeakReferenceMessenger.Default.Register<QueryVM, ConnectionsChangedMessage>(
            this, static (vm, _) => vm.StartAsBusy(vm.ReloadConnectionsAsync, blocking: false));
    }

    protected override async Task LoadDatasBusyAsync()
    {
        SyntaxConfiguration ??= await _syntaxConfigurations.GetAsync(SyntaxLanguage.Sql, LifetimeToken);

        await ReloadConnectionsAsync();

        if (Documents.Count == 0)
            New();
    }

    private async Task ReloadConnectionsAsync()
    {
        var stored = await _connectionStore.GetAllAsync(LifetimeToken);
        var activeId = ActiveConnection?.Id;

        // La sentinelle "Aucune connexion" reste en tete : c'est l'option de deconnexion.
        Connections = [NoConnection, .. stored];

        // Les descripteurs sont des clones : on reselectionne par Id, ou on deconnecte si supprime.
        SelectedConnection = stored.FirstOrDefault(c => c.Id == activeId) ?? NoConnection;
    }

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

    public void SetStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }
}
