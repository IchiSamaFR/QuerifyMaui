using SqlToolManagerMaui.ViewModels.Pages;

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
            var window = new Window(Ioc.GetRequiredService<AppShell>());

            // Fermeture : les onglets ouverts sont memorises pour etre rouverts au prochain demarrage.
            window.Destroying += (_, _) => Ioc.GetRequiredService<MainVM>().SaveWorkspace();

            return window;
        }
    }
}