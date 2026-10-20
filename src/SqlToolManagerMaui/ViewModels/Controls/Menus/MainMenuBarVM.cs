using CodeEdit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Services.Settings;
using SqlToolManagerMaui.ViewModels.Pages.Documents;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.ComponentModel;
using System.Windows.Input;

namespace SqlToolManagerMaui.ViewModels.Controls.Menus;

/// <summary>
/// Etat de la barre de menus declaree dans MainPage.xaml. Les menus Edition, Requete et Langage
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

    /// <summary>Null hors d'un onglet de script. Parametre : la valeur de <see cref="SyntaxLanguage"/>.</summary>
    [ObservableProperty]
    public partial ICommand? SetLanguageCommand { get; private set; }

    /// <summary>Commandes d'edition du CodeEditor de la vue affichee.</summary>
    [ObservableProperty]
    public partial CodeEditorCommands? EditionCommands { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSqlLanguage))]
    [NotifyPropertyChangedFor(nameof(IsLinqLanguage))]
    public partial SyntaxLanguage Language { get; private set; } = SyntaxLanguage.Sql;

    public bool IsSqlLanguage => Language == SyntaxLanguage.Sql;

    public bool IsLinqLanguage => Language == SyntaxLanguage.Linq;

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
                ExecuteCommand = _codeEditor.ExecuteQueryCommand;
                CancelCommand = _codeEditor.ExecuteQueryCancelCommand;
                SetLanguageCommand = _codeEditor.SetLanguageCommand;
                break;
            case DashboardSession:
                ExecuteCommand = _dashboard.ApplyParametersCommand;
                CancelCommand = _dashboard.CancelLoadingCommand;
                SetLanguageCommand = null;
                break;
            default:
                ExecuteCommand = null;
                CancelCommand = null;
                SetLanguageCommand = null;
                break;
        }

        RefreshEditionCommands();
        RefreshLanguage();
    }

    private void OnEditorVMPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CodeEditorVM.EditionCommands))
            RefreshEditionCommands();
    }

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorDocument.Language))
            RefreshLanguage();
    }

    private void RefreshEditionCommands()
        => EditionCommands = _session switch
        {
            CodeEditorSession => _codeEditor.EditionCommands,
            DashboardSession => _dashboard.EditionCommands,
            _ => null,
        };

    private void RefreshLanguage()
        => Language = _session is CodeEditorSession session ? session.Document.Language : SyntaxLanguage.Sql;

    // ---------- Commandes des preferences ----------

    [RelayCommand]
    private void ToggleRestoreOpenTabs() => Preferences.RestoreOpenTabs = !Preferences.RestoreOpenTabs;

    [RelayCommand]
    private void ToggleObjectExplorer() => Preferences.Panels.IsObjectExplorerOpen = !Preferences.Panels.IsObjectExplorerOpen;

    [RelayCommand]
    private void ToggleLineNumbers() => Preferences.Editor.IsLineDisplayed = !Preferences.Editor.IsLineDisplayed;

    [RelayCommand]
    private void ToggleWordWrap() => Preferences.Editor.IsWordWrapped = !Preferences.Editor.IsWordWrapped;

    [RelayCommand]
    private void ToggleTabIndentation() => Preferences.Editor.IsTabIndentation = !Preferences.Editor.IsTabIndentation;
}
