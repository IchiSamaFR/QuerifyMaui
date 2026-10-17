using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Services.Contexts;

namespace SqlToolManagerMaui.ViewModels.Controls.Menus;

[AutoRegister]
public partial class MainMenuBarVM : ObservableObject
{
    public FileMenuContext FileMenuContext { get; }
    public EditMenuContext EditMenuContext { get; }
    public ViewMenuContext ViewMenuContext { get; }
    public QueryMenuContext QueryMenuContext { get; }

    [ObservableProperty]
    public partial IList<MenuBarItem> MenuItems { get; set; } = Array.Empty<MenuBarItem>();

    public MainMenuBarVM(FileMenuContext fileMenuContext,
        EditMenuContext editMenuContext,
        ViewMenuContext viewMenuContext,
        QueryMenuContext queryMenuContext)
    {
        FileMenuContext = fileMenuContext;
        EditMenuContext = editMenuContext;
        ViewMenuContext = viewMenuContext;
        QueryMenuContext = queryMenuContext;

        MenuItems = new List<MenuBarItem>()
        {
            FileMenuContext.CreateMenu(),
            EditMenuContext.CreateMenu(),
            ViewMenuContext.CreateMenu(),
            QueryMenuContext.CreateMenu()
        };
    }
}
