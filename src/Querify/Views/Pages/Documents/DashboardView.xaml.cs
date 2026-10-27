using CommunityToolkit.Maui;
using Querify.Core.Models.Dashboards;
using Querify.ViewModels.Pages.Documents;
using Querify.Diagnostics;

namespace Querify.Views.Pages.Documents;

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

        PerfTrace.StartWatchdog(Dispatcher);
    }

    private static void OnSessionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (DashboardView)bindable;
        using (PerfTrace.Measure("DashboardView : affectation Session au VM"))
            view._viewModel.Session = newValue as DashboardSession;

        // Mesure jusqu'au prochain tour du dispatcher : inclut la creation des cartes et la mise en page.
        var start = System.Diagnostics.Stopwatch.StartNew();
        view.Dispatcher.Dispatch(() => PerfTrace.Mark($"DashboardView : premier tour UI apres Session ({start.ElapsedMilliseconds} ms)"));
    }
}
