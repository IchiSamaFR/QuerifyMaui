using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.UI.Constants;
using System.Windows.Input;

namespace SqlToolManagerMaui.Services.Contexts;

[AutoRegister]
public partial class ExecuteMenuContext : AbstractMenuContext
{
    private MenuBarItem _menu;

    // Observables : les commandes sont fournies par la page apres la creation du menu.
    [ObservableProperty]
    public partial ICommand? ExecuteCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? CancelCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? DisconnectCommand { get; set; }

    public override MenuBarItem CreateMenu()
    {
        _menu ??= CreateItem("Requête", new List<IMenuFlyoutItem>
        {
            CreateBoundItem("Exécuter", nameof(ExecuteCommand), KeyboardShortcutConstants.ExecuteQuery),
            CreateBoundItem("Annuler l'exécution", nameof(CancelCommand)),
            CreateSeparator(),
            CreateBoundItem("Se déconnecter", nameof(DisconnectCommand))
        });
        return _menu;
    }
}
