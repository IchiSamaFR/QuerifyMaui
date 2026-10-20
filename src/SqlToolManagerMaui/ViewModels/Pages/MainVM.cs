using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Languages.Queries;
using SqlToolManagerMaui.Services.Dialogs;
using SqlToolManagerMaui.Services.Editors;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.Services.Settings;
using SqlToolManagerMaui.UI.ViewModels;
using SqlToolManagerMaui.ViewModels.Controls.Menus;
using SqlToolManagerMaui.ViewModels.Pages.Documents;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.Collections.ObjectModel;

namespace SqlToolManagerMaui.ViewModels.Pages;

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
        IDialogService dialogs)
    {
        _files = files;
        _syntaxConfigurations = syntaxConfigurations;
        _dashboard = dashboard;
        _connection = connection;
        _status = status;
        _workspace = workspace;
        _preferencesStore = preferences;
        _dialogs = dialogs;
        Preferences = preferences.Preferences;
        MainMenuBar = mainMenuBar;

        WeakReferenceMessenger.Default.Register<MainVM, InsertQueryMessage>(this, static (vm, message) => vm.InsertQuery(message));
    }

    protected override async Task LoadDatasBusyAsync()
    {
        await _preferencesStore.LoadAsync(LifetimeToken);
        await RestoreWorkspaceAsync(LifetimeToken);
        await _connection.RefreshAsync(LifetimeToken);

        if (Sessions.Count == 0)
            New();
    }

    [RelayCommand]
    private void CloseObjectExplorer() => Preferences.Panels.IsObjectExplorerOpen = false;

    partial void OnSelectedSessionChanged(AbstractSession? value) => MainMenuBar.AttachSession(value);

    /// <summary>Ajoute l'onglet et le selectionne.</summary>
    private void Add(AbstractSession session)
    {
        Sessions.Add(session);
        SelectedSession = session;
    }

    /// <summary>La requete de l'explorateur part dans l'onglet de script courant (dans son langage), ou dans un nouveau.</summary>
    private void InsertQuery(InsertQueryMessage message)
    {
        if (SelectedSession is not CodeEditorSession)
            New();

        if (SelectedSession is not CodeEditorSession session)
            return;

        _connection.Select(message.Connection.Id);
        var document = session.Document;
        document.InsertBlock(document.Language == SyntaxLanguage.Linq ? message.ToLinq() : message.ToSql());
    }
}
