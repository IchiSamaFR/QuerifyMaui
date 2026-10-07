using CommunityToolkit.Maui;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors;

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
    [BindableProperty]
    public partial ICommand? OpenCommand { get; set; }

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
