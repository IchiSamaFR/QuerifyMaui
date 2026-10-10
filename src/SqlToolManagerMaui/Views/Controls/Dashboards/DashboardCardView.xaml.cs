using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Models.Dashboards;

namespace SqlToolManagerMaui.Views.Controls.Dashboards;

/// <summary>Une carte du dashboard : titre, puis tableau, graphique, valeur, texte ou bouton selon son rendu.</summary>
public partial class DashboardCardView : ContentView
{
    [BindableProperty]
    public partial DashboardCardState? Card { get; set; }

    public DashboardCardView()
    {
        InitializeComponent();
    }
}
