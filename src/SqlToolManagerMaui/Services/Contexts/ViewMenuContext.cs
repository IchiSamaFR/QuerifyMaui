using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.ViewModels.Pages.Queries;
using System.ComponentModel;
using System.Windows.Input;

namespace SqlToolManagerMaui.Services.Contexts;

/// <summary>Observable : les entrees cochees du menu Affichage sont liees a ces etats.</summary>
[AutoRegister]
public partial class ViewMenuContext : AbstractMenuContext
{
    private readonly EditorPreferencesVM _editorPreferences;

    private MenuBarItem _menu;

    public ICommand? ToggleObjectExplorerCommand { get; set; }
    public ICommand? ToggleLineNumbersCommand { get; set; }
    public ICommand? ToggleWordWrapCommand { get; set; }
    public ICommand? ToggleTabIndentationCommand { get; set; }
    public ICommand? SetLanguageCommand { get; set; }

    public bool IsObjectExplorerOpen => _editorPreferences.IsObjectExplorerOpen;

    public bool AreLineNumbersVisible => _editorPreferences.EditorConfiguration.IsLineDisplayed;

    public bool IsWordWrapEnabled => _editorPreferences.EditorConfiguration.IsWordWrapped;

    public bool IsTabIndentationEnabled => _editorPreferences.EditorConfiguration.IsTabIndentation;

    [ObservableProperty]
    public partial SyntaxLanguage Language { get; set; } = SyntaxLanguage.Sql;

    public ViewMenuContext(EditorPreferencesVM editorPreferences)
    {
        _editorPreferences = editorPreferences;

        ToggleObjectExplorerCommand = new Command(() => _editorPreferences.ToggleObjectExplorerCommand.Execute(null));
        ToggleLineNumbersCommand = new Command(() => _editorPreferences.ToggleLineNumbersCommand.Execute(null));
        ToggleWordWrapCommand = new Command(() => _editorPreferences.ToggleWordWrapCommand.Execute(null));
        ToggleTabIndentationCommand = new Command(() => _editorPreferences.ToggleUseTabsCommand.Execute(null));

        _editorPreferences.PropertyChanged += OnPreferencesChanged;
        _editorPreferences.EditorConfiguration.PropertyChanged += OnEditorConfigurationChanged;
    }

    private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorPreferencesVM.IsObjectExplorerOpen))
            OnPropertyChanged(nameof(IsObjectExplorerOpen));
    }

    private void OnEditorConfigurationChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Notifie toutes les entrees : le nom de propriete source peut differer.
        OnPropertyChanged(nameof(AreLineNumbersVisible));
        OnPropertyChanged(nameof(IsWordWrapEnabled));
        OnPropertyChanged(nameof(IsTabIndentationEnabled));
    }

    public override MenuBarItem CreateMenu()
    {
        var isEqual = new IsEqualConverter();
        _menu ??= CreateItem("Affichage", new List<IMenuFlyoutItem>
        {
            CreateCheckedItem("Explorateur d'objets", ToggleObjectExplorerCommand, this, nameof(ViewMenuContext.IsObjectExplorerOpen)),
            CreateSeparator(),
            CreateCheckedItem("Numéros de ligne", ToggleLineNumbersCommand, this, nameof(ViewMenuContext.AreLineNumbersVisible)),
            CreateCheckedItem("Retour à la ligne", ToggleWordWrapCommand, this, nameof(ViewMenuContext.IsWordWrapEnabled)),
            CreateCheckedItem("Indentation par tabulations", ToggleTabIndentationCommand, this, nameof(ViewMenuContext.IsTabIndentationEnabled)),
            CreateSeparator(),
            CreateSubMenu("Langage", new List<IMenuFlyoutItem>
            {
                CreateCheckedItem("SQL (T-SQL)", SetLanguageCommand, this, nameof(ViewMenuContext.Language), isEqual, SyntaxLanguage.Sql, SyntaxLanguage.Sql),
                CreateCheckedItem("LINQ (Entity Framework)", SetLanguageCommand, this, nameof(ViewMenuContext.Language), isEqual, SyntaxLanguage.Linq, SyntaxLanguage.Linq)
            })
        });
        return _menu;
    }
}
