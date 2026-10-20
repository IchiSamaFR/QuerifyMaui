using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Services.Contexts.Menus;
using SqlToolManagerMaui.UI.Constants;
using System.Windows.Input;

namespace SqlToolManagerMaui.Services.Contexts;

[AutoRegister]
public partial class FileMenuContext : AbstractMenuContext
{
    private MenuBarItem _menu;

    // Observables : les commandes sont fournies par la page apres la creation du menu.
    [ObservableProperty]
    public partial ICommand? NewFileCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? NewDashboardFileCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? OpenFileCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? SaveFileCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? SaveAsFileCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? CloseFileCommand { get; set; }

    /// <summary>Rouvre un fichier recent ; parametre : son chemin.</summary>
    [ObservableProperty]
    public partial ICommand? OpenRecentFilesCommand { get; set; }

    [ObservableProperty]
    public partial ICommand? ClearFilesCommand { get; set; }

    [ObservableProperty]
    public partial IEnumerable<string>? RecentFiles { get; set; }

    public override MenuBarItem CreateMenu()
    {
        _menu ??= CreateItem("Fichier", new List<IMenuFlyoutItem>
        {
            CreateBoundItem("Nouveau fichier", nameof(NewFileCommand), KeyboardShortcutConstants.NewFile),
            CreateBoundItem("Nouveau tableau de bord", nameof(NewDashboardFileCommand), KeyboardShortcutConstants.NewDashboardFile),
            CreateBoundItem("Ouvrir", nameof(OpenFileCommand), KeyboardShortcutConstants.OpenFile),
            CreateBoundSubItem("Fichiers récents", "Aucun fichier récent", nameof(RecentFiles), nameof(OpenRecentFilesCommand), nameof(ClearFilesCommand)),
            CreateSeparator(),
            CreateBoundItem("Enregistrer", nameof(SaveFileCommand), KeyboardShortcutConstants.SaveFile),
            CreateBoundItem("Enregistrer sous", nameof(SaveAsFileCommand)),
            CreateSeparator(),
            CreateBoundItem("Fermer", nameof(CloseFileCommand))
        });
        return _menu;
    }
}
