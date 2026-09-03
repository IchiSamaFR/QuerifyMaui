using MauiIcons.Fluent;
using Microsoft.Extensions.Logging;

namespace SqlToolManagerMaui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .UseFluentMauiIcons();

            builder.Services.AddApplicationServices();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            MauiApp app = builder.Build();
            // Locator statique, pour le peu de code qui ne peut pas recevoir ses dépendances.
            Ioc.Initialize(app.Services);
            return app;
        }
    }
}
