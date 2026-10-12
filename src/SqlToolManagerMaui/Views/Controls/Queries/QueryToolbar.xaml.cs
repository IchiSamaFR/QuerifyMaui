using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using System.Windows.Input;
using DashboardModeKind = SqlToolManagerMaui.Core.Enums.Dashboards.DashboardMode;

namespace SqlToolManagerMaui.Views.Controls.Queries;

/// <summary>
/// Barre de la page Requete, commune a l'editeur et au dashboard : execution (onglet de requete),
/// choix de la connexion, puis bascule Dashboard / Concepteur / Source (onglet .schx).
/// </summary>
public partial class QueryToolbar : ContentView
{
    /// <summary>Onglet .schx : masque l'execution, affiche la bascule de vue.</summary>
    [BindableProperty]
    public partial bool IsDashboard { get; set; }

    /// <summary>Choix de la liste deroulante des vues : la cle est la vue, la valeur son libelle.</summary>
    public static Dictionary<DashboardModeKind, string> DashboardModes { get; } = new()
    {
        [DashboardModeKind.View] = "Dashboard",
        [DashboardModeKind.Design] = "Concepteur",
        [DashboardModeKind.Source] = "Source",
    };

    /// <summary>Vue active du dashboard (onglet courant), affichee dans la liste deroulante.</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnDashboardModePropertyChanged))]
    public partial DashboardModeKind? DashboardMode { get; set; }

    /// <summary>
    /// Cle choisie dans la liste deroulante (interne). Un choix de l'utilisateur passe par la commande
    /// de la vue correspondante : le ViewModel garde la main (reecriture du JSON, relecture...).
    /// </summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSelectedDashboardModePropertyChanged))]
    public partial object? SelectedDashboardMode { get; set; }

    /// <summary>Vrai pendant que la liste suit DashboardMode : ce n'est pas un choix de l'utilisateur.</summary>
    private bool _isSyncingMode;

    [BindableProperty]
    public partial ICommand? ExecuteCommand { get; set; }

    [BindableProperty]
    public partial ICommand? CancelCommand { get; set; }

    /// <summary>Bouton Executer sur un onglet .schx : applique les parametres et relit les cartes.</summary>
    [BindableProperty]
    public partial ICommand? DashboardExecuteCommand { get; set; }

    /// <summary>Bouton Arreter sur un onglet .schx : interrompt la lecture des cartes.</summary>
    [BindableProperty]
    public partial ICommand? DashboardCancelCommand { get; set; }

    [BindableProperty]
    public partial ICommand? DisconnectCommand { get; set; }

    [BindableProperty]
    public partial IEnumerable<DatabaseConnectionInfo>? Connections { get; set; }

    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial DatabaseConnectionInfo? SelectedConnection { get; set; }

    [BindableProperty]
    public partial ICommand? ShowViewCommand { get; set; }

    [BindableProperty]
    public partial ICommand? ShowDesignerCommand { get; set; }

    [BindableProperty]
    public partial ICommand? ShowSourceCommand { get; set; }

    public QueryToolbar()
    {
        InitializeComponent();
    }

    private static void OnDashboardModePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((QueryToolbar)bindable).SyncSelectedDashboardMode();

    private static void OnSelectedDashboardModePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((QueryToolbar)bindable).OnSelectedDashboardModeChanged();

    private void SyncSelectedDashboardMode()
    {
        _isSyncingMode = true;
        SelectedDashboardMode = DashboardMode;
        _isSyncingMode = false;
    }

    private void OnSelectedDashboardModeChanged()
    {
        if (_isSyncingMode || SelectedDashboardMode is not DashboardModeKind mode || mode == DashboardMode)
            return;

        var command = mode switch
        {
            DashboardModeKind.View => ShowViewCommand,
            DashboardModeKind.Design => ShowDesignerCommand,
            _ => ShowSourceCommand,
        };

        if (command?.CanExecute(null) == true)
            command.Execute(null);

        // Commande refusee (ou sans effet) : la liste revient sur la vue reellement affichee.
        if (DashboardMode != mode)
            Dispatcher.Dispatch(SyncSelectedDashboardMode);
    }
}
