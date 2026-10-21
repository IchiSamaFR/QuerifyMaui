using CommunityToolkit.Maui;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Dashboards;

/// <summary>Onglet d'une page dans le concepteur : clic = ouverture ; la page editee se renomme et se supprime sur place.</summary>
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

    /// <summary>Transmis a <see cref="OpenCommand"/> et <see cref="DeleteCommand"/> (la page).</summary>
    [BindableProperty]
    public partial object? CommandParameter { get; set; }

    public DashboardPageTab()
    {
        InitializeComponent();
    }
}
