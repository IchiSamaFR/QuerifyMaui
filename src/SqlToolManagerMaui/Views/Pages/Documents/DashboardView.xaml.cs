using CommunityToolkit.Maui;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.ViewModels.Pages.Documents;

namespace SqlToolManagerMaui.Views.Pages.Documents;

/// <summary>
/// Onglet .schx : en-tete, puis grille de cartes, concepteur ou JSON selon la vue. Le contenu est lie au
/// <see cref="DashboardVM"/> singleton ; <see cref="Session"/> lui transmet l'onglet affiche.
/// </summary>
public partial class DashboardView : ContentView
{
    private readonly DashboardVM _viewModel;

    /// <summary>Session de l'onglet affiche (null quand la vue est masquee).</summary>
    [BindableProperty(PropertyChangedMethodName = nameof(OnSessionPropertyChanged))]
    public partial DashboardSession? Session { get; set; }

    public DashboardView()
    {
        InitializeComponent();

        // Vue creee par le DataTemplate de MainPage : pas d'injection par constructeur possible.
        _viewModel = Ioc.GetRequiredService<DashboardVM>();
        Body.BindingContext = _viewModel;
    }

    private static void OnSessionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (DashboardView)bindable;
        view._viewModel.Session = newValue as DashboardSession;
    }
}
