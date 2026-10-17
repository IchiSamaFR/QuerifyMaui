using SqlToolManagerMaui.Core.Ioc;
using System.Windows.Input;

namespace SqlToolManagerMaui.Services.Contexts;

[AutoRegister]
public class QueryMenuContext : AbstractMenuContext
{
    private MenuBarItem _menu;

    public ICommand? ExecuteCommand { get; set; }
    public ICommand? CancelCommand { get; set; }
    public ICommand? DisconnectCommand { get; set; }

    public override MenuBarItem CreateMenu()
    {
        _menu ??= CreateItem("Requête", new List<IMenuFlyoutItem>
        {
            CreateItem("Exécuter", ExecuteCommand),
            CreateItem("Annuler l'exécution", CancelCommand),
            CreateSeparator(),
            CreateItem("Se déconnecter", DisconnectCommand)
        });
        return _menu;
    }
}
