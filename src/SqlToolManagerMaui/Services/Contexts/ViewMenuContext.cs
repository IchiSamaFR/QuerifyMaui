using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Ioc;
using System.Windows.Input;

namespace SqlToolManagerMaui.Services.Contexts;

[AutoRegister]
public partial class ViewMenuContext : AbstractMenuContext
{
    private MenuBarItem _menu;

    // Observables : commandes et etats sont fournis par la page apres la creation du menu.
    [ObservableProperty]
    public partial ICommand? ToggleObjectExplorerCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? ToggleLineNumbersCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? ToggleWordWrapCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? ToggleTabIndentationCommand { get; set; }

    /// <summary>Parametre : la valeur de <see cref="SyntaxLanguage"/>.</summary>
    [ObservableProperty]
    public partial ICommand? SetLanguageCommand { get; set; }

    [ObservableProperty]
    public partial bool IsObjectExplorerOpen { get; set; }

    [ObservableProperty]
    public partial bool AreLineNumbersVisible { get; set; }

    [ObservableProperty]
    public partial bool IsWordWrapEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsTabIndentationEnabled { get; set; }

    [ObservableProperty]
    public partial SyntaxLanguage Language { get; set; } = SyntaxLanguage.Sql;

    public override MenuBarItem CreateMenu()
    {
        var isEqual = new IsEqualConverter();
        _menu ??= CreateItem("Affichage", new List<IMenuFlyoutItem>
        {
            CreateBoundCheckedItem("Explorateur d'objets", nameof(ToggleObjectExplorerCommand), nameof(IsObjectExplorerOpen)),
            CreateSeparator(),
            CreateBoundCheckedItem("Numéros de ligne", nameof(ToggleLineNumbersCommand), nameof(AreLineNumbersVisible)),
            CreateBoundCheckedItem("Retour à la ligne", nameof(ToggleWordWrapCommand), nameof(IsWordWrapEnabled)),
            CreateBoundCheckedItem("Indentation par tabulations", nameof(ToggleTabIndentationCommand), nameof(IsTabIndentationEnabled)),
            CreateSeparator(),
            CreateSubMenu("Langage", new List<IMenuFlyoutItem>
            {
                CreateBoundCheckedItem("SQL (T-SQL)", nameof(SetLanguageCommand), nameof(Language), isEqual, SyntaxLanguage.Sql, SyntaxLanguage.Sql),
                CreateBoundCheckedItem("LINQ (Entity Framework)", nameof(SetLanguageCommand), nameof(Language), isEqual, SyntaxLanguage.Linq, SyntaxLanguage.Linq)
            })
        });
        return _menu;
    }
}
