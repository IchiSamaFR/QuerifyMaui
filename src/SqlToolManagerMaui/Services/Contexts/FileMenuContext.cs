using SqlToolManagerMaui.Core.Ioc;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SqlToolManagerMaui.Services.Contexts;

[AutoRegister]
public partial class FileMenuContext : AbstractMenuContext
{
    private MenuBarItem _menu;

    public ICommand? NewFileCommand { get; set; }
    public ICommand? NewDashboardFileCommand { get; set; }
    public ICommand? OpenFileCommand { get; set; }
    public ICommand? SaveFileCommand { get; set; }
    public ICommand? SaveAsFileCommand { get; set; }
    public ICommand? CloseFileCommand { get; set; }

    public ICommand? OpenRecentFilesCommand { get; set; }
    public ICommand? ClearFilesCommand { get; set; }
    public ObservableCollection<string> RecentFiles { get; set; }

    public override MenuBarItem CreateMenu()
    {
        _menu ??= CreateItem("Fichier", new List<IMenuFlyoutItem>
        {
            CreateItem("Nouveau fichier", NewFileCommand),
            CreateItem("Nouveau tableau de bord", NewDashboardFileCommand),
            CreateItem("Ouvrir", OpenFileCommand),
            CreateSubItem("Fichiers récents", "Aucun fichier récent", RecentFiles, OpenRecentFilesCommand, ClearFilesCommand),
            CreateSeparator(),
            CreateItem("Enregistrer", SaveFileCommand),
            CreateItem("Enregistrer sous", SaveAsFileCommand),
            CreateSeparator(),
            CreateItem("Fermer", CloseFileCommand)
        });
        return _menu;
    }
}
