using CommunityToolkit.Maui;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors.Menus;

/// <summary>
/// Menu "Fichier" d'un editeur multi-onglets. Le controle ne connait aucun ViewModel :
/// la page hote branche chaque entree via les commandes exposees ci-dessous.
/// </summary>
public partial class FileMenuBarItem : MenuBarItem
{
    /// <summary>Nouveau document (Ctrl+N).</summary>
    [BindableProperty]
    public partial ICommand? NewCommand { get; set; }

    /// <summary>Ouvrir un fichier (Ctrl+O).</summary>
    /// <summary>Nouveau dashboard .schx (grille vide).</summary>
    [BindableProperty]
    public partial ICommand? NewDashboardCommand { get; set; }

    [BindableProperty]
    public partial ICommand? OpenCommand { get; set; }

    /// <summary>Chemins des fichiers recents, du plus recent au plus ancien.</summary>
    [BindableProperty]
    public partial IEnumerable<string>? RecentFiles { get; set; }

    /// <summary>Rouvre un fichier recent ; parametre : son chemin.</summary>
    [BindableProperty]
    public partial ICommand? OpenRecentCommand { get; set; }

    /// <summary>Vide la liste des fichiers recents.</summary>
    [BindableProperty]
    public partial ICommand? ClearRecentFilesCommand { get; set; }

    /// <summary>Enregistrer le document actif (Ctrl+S).</summary>
    [BindableProperty]
    public partial ICommand? SaveCommand { get; set; }

    /// <summary>Enregistrer sous (Ctrl+Maj+S).</summary>
    [BindableProperty]
    public partial ICommand? SaveAsCommand { get; set; }

    /// <summary>Fermer l'onglet actif (Ctrl+W).</summary>
    [BindableProperty]
    public partial ICommand? CloseTabCommand { get; set; }

    /// <summary>Quitter l'application.</summary>
    [BindableProperty]
    public partial ICommand? QuitCommand { get; set; }

    public FileMenuBarItem()
    {
        InitializeComponent();
    }
}
