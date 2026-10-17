using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Ioc;
using System.Windows.Input;

namespace SqlToolManagerMaui.Services.Contexts;

[AutoRegister]
public partial class EditMenuContext : AbstractMenuContext
{
    private MenuBarItem _menu;

    // Observables : les commandes arrivent de l'editeur apres la creation du menu.
    [ObservableProperty]
    public partial ICommand? UndoCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? RedoCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? CutCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? CopyCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? PasteCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? SelectAllCommand { get; set; }

    public override MenuBarItem CreateMenu()
    {
        _menu ??= CreateItem("Édition", new List<IMenuFlyoutItem>
        {
            CreateBoundItem("Annuler", nameof(UndoCommand)),
            CreateBoundItem("Rétablir", nameof(RedoCommand)),
            CreateSeparator(),
            CreateBoundItem("Couper", nameof(CutCommand)),
            CreateBoundItem("Copier", nameof(CopyCommand)),
            CreateBoundItem("Coller", nameof(PasteCommand)),
            CreateSeparator(),
            CreateBoundItem("Tout sélectionner", nameof(SelectAllCommand))
        });
        return _menu;
    }
}
