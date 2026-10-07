using CommunityToolkit.Maui;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Menu "Requete" : execution (F5), annulation et deconnexion. Les commandes sont fournies par la page hote.
/// </summary>
public partial class QueryMenuBarItem : MenuBarItem
{
    /// <summary>Execute la selection, ou tout le document (F5).</summary>
    [BindableProperty]
    public partial ICommand? ExecuteCommand { get; set; }

    /// <summary>Interrompt l'execution en cours.</summary>
    [BindableProperty]
    public partial ICommand? CancelCommand { get; set; }

    /// <summary>Ferme la connexion courante.</summary>
    [BindableProperty]
    public partial ICommand? DisconnectCommand { get; set; }

    public QueryMenuBarItem()
    {
        InitializeComponent();
    }
}
