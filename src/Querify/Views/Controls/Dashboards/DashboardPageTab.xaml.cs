using CommunityToolkit.Maui;
using System.Windows.Input;

namespace Querify.Views.Controls.Dashboards;

/// <summary>Onglet d'une page dans le concepteur : clic = ouverture ; la page editee se renomme sur place, ses options portent la suppression.</summary>
public partial class DashboardPageTab : ContentView
{
    [BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
    public partial string? Title { get; set; }

    /// <summary>Page affichee dans le concepteur.</summary>
    [BindableProperty]
    public partial bool IsCurrent { get; set; }

    [BindableProperty]
    public partial ICommand? OpenCommand { get; set; }

    [BindableProperty]
    public partial ICommand? DeleteCommand { get; set; }

    /// <summary>Transmis aux commandes de l'onglet (la page).</summary>
    [BindableProperty]
    public partial object? CommandParameter { get; set; }

    public DashboardPageTab()
    {
        InitializeComponent();
    }
}
