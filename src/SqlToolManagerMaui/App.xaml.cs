using SqlToolManagerMaui.ViewModels.Pages;
using SqlToolManagerMaui.Views.Controls.TitleBars;

namespace SqlToolManagerMaui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Palette calquee sur le theme Dark de WPF UI uniquement
            UserAppTheme = AppTheme.Dark;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(Ioc.GetRequiredService<AppShell>()) { TitleBar = new AppTitleBar() };
            var main = Ioc.GetRequiredService<MainVM>();

#if WINDOWS
            // Demande d'enregistrer les onglets modifies ; « Annuler » garde la fenetre ouverte.
            WinUI.WindowCloseGuard.Attach(window, main.CanCloseAsync);
#endif

            // Fermeture : les onglets ouverts sont memorises pour etre rouverts au prochain demarrage.
            window.Destroying += (_, _) => main.SaveWorkspace();

            return window;
        }
    }
}