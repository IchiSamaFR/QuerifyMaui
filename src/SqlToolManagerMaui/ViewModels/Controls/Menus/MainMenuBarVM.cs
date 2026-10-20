using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Services.Contexts;
using SqlToolManagerMaui.ViewModels.Pages;
using SqlToolManagerMaui.ViewModels.Pages.Documents;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.ComponentModel;

namespace SqlToolManagerMaui.ViewModels.Controls.Menus;

/// <summary>
/// Barre de menus de la page principale. Les menus Edition, Requete et Langage suivent
/// la session selectionnee (<see cref="AttachSession"/>) et le VM qui l'affiche.
/// </summary>
[AutoRegister]
public partial class MainMenuBarVM : ObservableObject
{
    private readonly EditorPreferencesVM _preferences;
    private readonly CodeEditorVM _codeEditor;
    private readonly DashboardVM _dashboard;
    private AbstractSession? _session;

    public FileMenuContext FileMenuContext { get; }
    public EditMenuContext EditMenuContext { get; }
    public ViewMenuContext ViewMenuContext { get; }
    public ExecuteMenuContext ExecuteMenuContext { get; }

    [ObservableProperty]
    public partial IList<MenuBarItem> MenuItems { get; set; } = Array.Empty<MenuBarItem>();

    public MainMenuBarVM(FileMenuContext fileMenuContext,
        EditMenuContext editMenuContext,
        ViewMenuContext viewMenuContext,
        ExecuteMenuContext executeMenuContext,
        EditorPreferencesVM preferences,
        DatabaseConnectionVM connection,
        CodeEditorVM codeEditor,
        DashboardVM dashboard)
    {
        FileMenuContext = fileMenuContext;
        EditMenuContext = editMenuContext;
        ViewMenuContext = viewMenuContext;
        ExecuteMenuContext = executeMenuContext;
        _preferences = preferences;
        _codeEditor = codeEditor;
        _dashboard = dashboard;

        MenuItems = new List<MenuBarItem>()
        {
            FileMenuContext.CreateMenu(),
            EditMenuContext.CreateMenu(),
            ViewMenuContext.CreateMenu(),
            ExecuteMenuContext.CreateMenu()
        };

        ExecuteMenuContext.DisconnectCommand = connection.DisconnectCommand;
        AttachPreferences();

        // Les commandes d'edition arrivent du CodeEditor de chaque vue apres sa creation.
        _codeEditor.PropertyChanged += OnEditorVMPropertyChanged;
        _dashboard.PropertyChanged += OnEditorVMPropertyChanged;
    }

    /// <summary>Commandes fichier de la page ; appele une fois, par MainVM.</summary>
    public void AttachFileCommands(MainVM main)
    {
        var menu = FileMenuContext;
        menu.NewFileCommand = main.NewCommand;
        menu.NewDashboardFileCommand = main.NewDashboardCommand;
        menu.OpenFileCommand = main.OpenCommand;
        menu.OpenRecentFilesCommand = main.OpenRecentCommand;
        menu.SaveFileCommand = main.SaveCommand;
        menu.SaveAsFileCommand = main.SaveAsCommand;
        menu.CloseFileCommand = main.CloseActiveCommand;
    }

    /// <summary>Branche les menus Edition, Requete et Langage sur la session selectionnee.</summary>
    public void AttachSession(AbstractSession? session)
    {
        if (_session is not null)
            _session.Document.PropertyChanged -= OnDocumentPropertyChanged;

        _session = session;

        if (session is not null)
            session.Document.PropertyChanged += OnDocumentPropertyChanged;

        switch (session)
        {
            case CodeEditorSession:
                ExecuteMenuContext.ExecuteCommand = _codeEditor.ExecuteQueryCommand;
                ExecuteMenuContext.CancelCommand = _codeEditor.ExecuteQueryCancelCommand;
                ViewMenuContext.SetLanguageCommand = _codeEditor.SetLanguageCommand;
                break;
            case DashboardSession:
                ExecuteMenuContext.ExecuteCommand = _dashboard.ApplyParametersCommand;
                ExecuteMenuContext.CancelCommand = _dashboard.CancelLoadingCommand;
                ViewMenuContext.SetLanguageCommand = null;
                break;
            default:
                ExecuteMenuContext.ExecuteCommand = null;
                ExecuteMenuContext.CancelCommand = null;
                ViewMenuContext.SetLanguageCommand = null;
                break;
        }

        RefreshEditMenu();
        RefreshLanguage();
    }

    private void OnEditorVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CodeEditorVM.EditionCommands))
            RefreshEditMenu();
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorDocument.Language))
            RefreshLanguage();
    }

    private void RefreshEditMenu()
    {
        var commands = _session switch
        {
            CodeEditorSession => _codeEditor.EditionCommands,
            DashboardSession => _dashboard.EditionCommands,
            _ => null,
        };

        var menu = EditMenuContext;
        menu.UndoCommand = commands?.Undo;
        menu.RedoCommand = commands?.Redo;
        menu.CutCommand = commands?.Cut;
        menu.CopyCommand = commands?.Copy;
        menu.PasteCommand = commands?.Paste;
        menu.SelectAllCommand = commands?.SelectAll;
    }

    private void RefreshLanguage()
        => ViewMenuContext.Language = _session is CodeEditorSession session ? session.Document.Language : SyntaxLanguage.Sql;

    private void AttachPreferences()
    {
        FileMenuContext.ClearFilesCommand = _preferences.ClearRecentFilesCommand;

        var view = ViewMenuContext;
        view.ToggleObjectExplorerCommand = _preferences.ToggleObjectExplorerCommand;
        view.ToggleLineNumbersCommand = _preferences.ToggleLineNumbersCommand;
        view.ToggleWordWrapCommand = _preferences.ToggleWordWrapCommand;
        view.ToggleTabIndentationCommand = _preferences.ToggleUseTabsCommand;

        RefreshPreferences();

        // RecentFiles est remplacee (pas modifiee) a chaque mise a jour des preferences.
        _preferences.PropertyChanged += (_, _) => RefreshPreferences();
        _preferences.EditorConfiguration.PropertyChanged += (_, _) => RefreshPreferences();
    }

    private void RefreshPreferences()
    {
        var configuration = _preferences.EditorConfiguration;
        FileMenuContext.RecentFiles = _preferences.RecentFiles;
        ViewMenuContext.IsObjectExplorerOpen = _preferences.IsObjectExplorerOpen;
        ViewMenuContext.AreLineNumbersVisible = configuration.IsLineDisplayed;
        ViewMenuContext.IsWordWrapEnabled = configuration.IsWordWrapped;
        ViewMenuContext.IsTabIndentationEnabled = configuration.IsTabIndentation;
    }
}
