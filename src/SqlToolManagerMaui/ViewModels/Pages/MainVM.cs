using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Queries;
using SqlToolManagerMaui.Services.Editors;
using SqlToolManagerMaui.Services.Editors.Highlighting;
using SqlToolManagerMaui.UI.ViewModels;
using SqlToolManagerMaui.ViewModels.Controls.Menus;
using SqlToolManagerMaui.ViewModels.Pages.Documents;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.Collections.ObjectModel;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>
/// Page principale : les onglets ouverts et le document selectionne. Chaque onglet est un
/// <see cref="DocumentVM"/> autonome, affiche par la vue que choisit le DataTemplateSelector de la page.
/// </summary>
[AutoRegister]
public partial class MainVM : AbstractVM
{
    private readonly IEditorFileService _files;
    private readonly ISyntaxConfigurationStore _syntaxConfigurations;
    private readonly DocumentVMFactory _documentFactory;
    private readonly DatabaseConnectionVM _connection;
    private readonly StatusBarVM _status;

    public MainVM(IEditorFileService files,
        ISyntaxConfigurationStore syntaxConfigurations,
        DocumentVMFactory documentFactory,
        DatabaseConnectionVM connection,
        StatusBarVM status,
        EditorPreferencesVM preferences,
        MainMenuBarVM mainMenuBar)
    {
        _files = files;
        _syntaxConfigurations = syntaxConfigurations;
        _documentFactory = documentFactory;
        _connection = connection;
        _status = status;
        Preferences = preferences;
        MainMenuBar = mainMenuBar;

        MainMenuBar.AttachFileCommands(this);
        WeakReferenceMessenger.Default.Register<MainVM, InsertQueryMessage>(this, static (vm, message) => vm.InsertQuery(message));
    }

    /// <summary>Onglets ouverts, dans l'ordre d'ouverture.</summary>
    public ObservableCollection<DocumentVM> Documents { get; } = new();

    [ObservableProperty]
    public partial DocumentVM? SelectedDocument { get; set; }

    /// <summary>Volet de l'explorateur d'objets (disposition de la page).</summary>
    public EditorPreferencesVM Preferences { get; }

    public MainMenuBarVM MainMenuBar { get; }

    protected override async Task LoadDatasBusyAsync()
    {
        await Preferences.LoadAsync(LifetimeToken);
        await _connection.RefreshAsync(LifetimeToken);

        if (Documents.Count == 0)
            New();
    }

    partial void OnSelectedDocumentChanged(DocumentVM? oldValue, DocumentVM? newValue)
    {
        if (oldValue is not null)
            oldValue.IsActive = false;

        if (newValue is not null)
            newValue.IsActive = true;

        MainMenuBar.AttachDocument(newValue);
    }

    /// <summary>Ajoute l'onglet et le selectionne.</summary>
    private void Add(DocumentVM document)
    {
        Documents.Add(document);
        SelectedDocument = document;
        document.OnOpened();
    }

    /// <summary>La requete de l'explorateur part dans l'onglet de script courant, ou dans un nouveau.</summary>
    private void InsertQuery(InsertQueryMessage message)
    {
        if (SelectedDocument is not CodeEditorVM)
            New();

        (SelectedDocument as CodeEditorVM)?.InsertQuery(message);
    }
}
