using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Services.Contexts;
using SqlToolManagerMaui.ViewModels.Pages;
using SqlToolManagerMaui.ViewModels.Pages.Documents;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.ComponentModel;

namespace SqlToolManagerMaui.ViewModels.Controls.Menus;

/// <summary>
/// Barre de menus de la page principale. Les menus Edition, Requete et Langage suivent
/// l'onglet selectionne (<see cref="AttachDocument"/>).
/// </summary>
[AutoRegister]
public partial class MainMenuBarVM : ObservableObject
{
    private readonly EditorPreferencesVM _preferences;
    private DocumentVM? _document;

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
        DatabaseConnectionVM connection)
    {
        FileMenuContext = fileMenuContext;
        EditMenuContext = editMenuContext;
        ViewMenuContext = viewMenuContext;
        ExecuteMenuContext = executeMenuContext;
        _preferences = preferences;

        MenuItems = new List<MenuBarItem>()
        {
            FileMenuContext.CreateMenu(),
            EditMenuContext.CreateMenu(),
            ViewMenuContext.CreateMenu(),
            ExecuteMenuContext.CreateMenu()
        };

        ExecuteMenuContext.DisconnectCommand = connection.DisconnectCommand;
        AttachPreferences();
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

    /// <summary>Branche les menus Edition, Requete et Langage sur l'onglet selectionne.</summary>
    public void AttachDocument(DocumentVM? document)
    {
        if (_document is not null)
            _document.PropertyChanged -= OnDocumentPropertyChanged;

        _document = document;

        if (document is not null)
            document.PropertyChanged += OnDocumentPropertyChanged;

        ExecuteMenuContext.ExecuteCommand = document?.ExecuteCommand;
        ExecuteMenuContext.CancelCommand = document?.CancelCommand;
        ViewMenuContext.SetLanguageCommand = (document as CodeEditorVM)?.SetLanguageCommand;

        RefreshEditMenu();
        RefreshLanguage();
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DocumentVM.EditionCommands):
                RefreshEditMenu();
                break;
            case nameof(CodeEditorVM.Language):
                RefreshLanguage();
                break;
        }
    }

    private void RefreshEditMenu()
    {
        var commands = _document?.EditionCommands;
        var menu = EditMenuContext;
        menu.UndoCommand = commands?.Undo;
        menu.RedoCommand = commands?.Redo;
        menu.CutCommand = commands?.Cut;
        menu.CopyCommand = commands?.Copy;
        menu.PasteCommand = commands?.Paste;
        menu.SelectAllCommand = commands?.SelectAll;
    }

    private void RefreshLanguage()
        => ViewMenuContext.Language = (_document as CodeEditorVM)?.Language ?? SyntaxLanguage.Sql;

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
