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
            return new Window(Ioc.GetRequiredService<AppShell>());
        }
    }
}