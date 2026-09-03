using Microsoft.Extensions.DependencyInjection;

namespace SqlToolManagerMaui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(Ioc.GetRequiredService<AppShell>());
        }
    }
}