using Querify.Core.Services.Editors.Highlighting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors;
using Querify.Core.Models.Settings;
using Querify.Languages.Editors;
using Querify.Languages.Queries;
using Querify.Services.Dialogs;
using Querify.Services.Editors;
using Querify.Services.Editors.Highlighting;
using Querify.Services.Settings;
using Querify.UI.ViewModels;
using Querify.ViewModels.Controls.Menus;
using Querify.ViewModels.Pages.Documents;
using Querify.ViewModels.Pages.Shared;
using System.Collections.ObjectModel;

namespace Querify.ViewModels.Pages;

/// <summary>
/// Page principale : les onglets ouverts (une <see cref="AbstractSession"/> chacun) et la session selectionnee.
/// La vue de la session est choisie par le DocumentViewSelector de la page et lui transmet la session.
/// </summary>
[AutoRegister]
public partial class MainVM : AbstractVM
{
    private readonly IEditorFileService _files;
    private readonly ISyntaxConfigurationStore _syntaxConfigurations;
    private readonly DashboardVM _dashboard;
    private readonly DatabaseConnectionVM _connection;
    private readonly StatusBarVM _status;
    private readonly IWorkspaceStore _workspace;
    private readonly IUserPreferencesStore _preferencesStore;
    private readonly IDialogService _dialogs;
    private readonly IDocumentTemplateStore _templates;
    private readonly IEditorLanguageResolver _languages;

    /// <summary>Onglets ouverts, dans l'ordre d'ouverture.</summary>
    [ObservableProperty]
    public partial ObservableCollection<AbstractSession> Sessions { get; set; } = new();

    [ObservableProperty]
    public partial AbstractSession? SelectedSession { get; set; }

    /// <summary>Volet de l'explorateur d'objets (disposition de la page).</summary>
    public UserPreferences Preferences { get; }

    public MainMenuBarVM MainMenuBar { get; }

    public MainVM(IEditorFileService files,
        ISyntaxConfigurationStore syntaxConfigurations,
        DashboardVM dashboard,
        DatabaseConnectionVM connection,
        StatusBarVM status,
        IUserPreferencesStore preferences,
        MainMenuBarVM mainMenuBar,
        IWorkspaceStore workspace,
        IDialogService dialogs,
        IDocumentTemplateStore templates,
        IEditorLanguageResolver languages)
    {
        _files = files;
        _syntaxConfigurations = syntaxConfigurations;
        _dashboard = dashboard;
        _connection = connection;
        _status = status;
        _workspace = workspace;
        _preferencesStore = preferences;
        _dialogs = dialogs;
        _templates = templates;
        _languages = languages;
        Preferences = preferences.Preferences;
        MainMenuBar = mainMenuBar;

        // Changement de connexion par l'utilisateur : retenue pour le fichier de l'onglet.
        _connection.SelectedConnectionChanged += (_, _) => RememberConnection(SelectedSession);

        WeakReferenceMessenger.Default.Register<MainVM, InsertQueryMessage>(this, static (vm, message) => vm.InsertQuery(message));
    }

    protected override async Task LoadDatasBusyAsync()
    {
        await _preferencesStore.LoadAsync(LifetimeToken);
        await RestoreWorkspaceAsync(LifetimeToken);
        await _connection.RefreshAsync(LifetimeToken);

        // Premier lancement (ou aucun onglet a restaurer) : le modele guide la prise en main.
        if (Sessions.Count == 0)
            await NewAsync(SyntaxLanguage.Sql);
    }

    [RelayCommand]
    private void CloseObjectExplorer() => Preferences.Panels.IsObjectExplorerOpen = false;

    partial void OnSelectedSessionChanged(AbstractSession? value)
    {
        _connection.AttachSession(value);
        MainMenuBar.AttachSession(value);
    }

    /// <summary>Ajoute l'onglet et le selectionne ; sans connexion propre, il prend celle de l'onglet courant.</summary>
    private void Add(AbstractSession session)
    {
        session.ConnectionId ??= _connection.ActiveConnection?.Id;
        Sessions.Add(session);
        SelectedSession = session;
    }

    /// <summary>La requete de l'explorateur part dans l'onglet de script courant (dans son langage), ou dans un nouveau.</summary>
    private void InsertQuery(InsertQueryMessage message)
    {
        if (SelectedSession is not CodeEditorSession)
            AddUntitled();

        if (SelectedSession is not CodeEditorSession session)
            return;

        _connection.Select(message.Connection.Id);
        var document = session.Document;

        // Un onglet sans langage d'editeur recoit du SQL, ecrit pour le moteur de la connexion.
        if ((_languages.Find(document.Language) ?? _languages.Find(SyntaxLanguage.Sql)) is { } language)
            document.InsertBlock(language.CreateSelectFirstRows(message));
    }
}
