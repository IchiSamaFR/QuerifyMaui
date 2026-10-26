using CodeEdit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Querify.Core.Ioc;
using Querify.Core.Models.Dashboards;
using Querify.Core.Models.Editors;
using Querify.Core.Models.Settings;
using Querify.Services.Settings;
using Querify.ViewModels.Pages.Documents;
using Querify.ViewModels.Pages.Shared;
using System.ComponentModel;
using System.Windows.Input;

namespace Querify.ViewModels.Controls.Menus;

/// <summary>
/// Etat de la barre de menus declaree dans MainPage.xaml. Les menus Edition et Requete
/// suivent la session selectionnee (<see cref="AttachSession"/>) et le VM qui l'affiche ; les
/// commandes Fichier sont liees directement a MainVM.
/// </summary>
[AutoRegister]
public partial class MainMenuBarVM : ObservableObject
{
    private readonly CodeEditorVM _codeEditor;
    private readonly DashboardVM _dashboard;
    private AbstractSession? _session;

    public UserPreferences Preferences { get; }

    public DatabaseConnectionVM Connection { get; }

    /// <summary>Execution de la session affichee (requete ou relecture des cartes).</summary>
    [ObservableProperty]
    public partial ICommand? ExecuteCommand { get; private set; }

    [ObservableProperty]
    public partial ICommand? CancelCommand { get; private set; }

    /// <summary>Commandes d'edition du CodeEditor de la vue affichee.</summary>
    [ObservableProperty]
    public partial CodeEditorCommands? EditionCommands { get; private set; }

    public MainMenuBarVM(IUserPreferencesStore preferences,
        DatabaseConnectionVM connection,
        CodeEditorVM codeEditor,
        DashboardVM dashboard)
    {
        Preferences = preferences.Preferences;
        Connection = connection;
        _codeEditor = codeEditor;
        _dashboard = dashboard;

        // Les commandes d'edition arrivent du CodeEditor de chaque vue apres sa creation.
        _codeEditor.PropertyChanged += OnEditorVMPropertyChanged;
        _dashboard.PropertyChanged += OnEditorVMPropertyChanged;
    }

    /// <summary>Branche les menus Edition et Requete sur la session selectionnee.</summary>
    public void AttachSession(AbstractSession? session)
    {
        _session = session;

        switch (session)
        {
            case CodeEditorSession:
                ExecuteCommand = _codeEditor.ExecuteQueryCommand;
                CancelCommand = _codeEditor.ExecuteQueryCancelCommand;
                break;
            case DashboardSession:
                ExecuteCommand = _dashboard.ApplyParametersCommand;
                CancelCommand = _dashboard.CancelLoadingCommand;
                break;
            default:
                ExecuteCommand = null;
                CancelCommand = null;
                break;
        }

        RefreshEditionCommands();
    }

    private void OnEditorVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CodeEditorVM.EditionCommands))
            RefreshEditionCommands();
    }

    private void RefreshEditionCommands()
        => EditionCommands = _session switch
        {
            CodeEditorSession => _codeEditor.EditionCommands,
            DashboardSession => _dashboard.EditionCommands,
            _ => null,
        };

    // ---------- Commandes des preferences ----------

    [RelayCommand]
    private void ToggleRestoreOpenTabs() => Preferences.RestoreOpenTabs = !Preferences.RestoreOpenTabs;

    [RelayCommand]
    private void ToggleInsertTemplate() => Preferences.Editor.InsertTemplateOnNew = !Preferences.Editor.InsertTemplateOnNew;

    [RelayCommand]
    private void ToggleObjectExplorer() => Preferences.Panels.IsObjectExplorerOpen = !Preferences.Panels.IsObjectExplorerOpen;

    [RelayCommand]
    private void ToggleLineNumbers() => Preferences.Editor.IsLineDisplayed = !Preferences.Editor.IsLineDisplayed;

    [RelayCommand]
    private void ToggleWordWrap() => Preferences.Editor.IsWordWrapped = !Preferences.Editor.IsWordWrapped;

    [RelayCommand]
    private void ToggleTabIndentation() => Preferences.Editor.IsTabIndentation = !Preferences.Editor.IsTabIndentation;
}
