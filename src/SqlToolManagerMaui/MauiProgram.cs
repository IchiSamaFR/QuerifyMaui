using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using MauiIcons.Fluent;
using Microsoft.Extensions.Logging;

namespace SqlToolManagerMaui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Force le chargement de SqlToolManagerMaui.UI AVANT le premier parsing XAML.
            // Les ResourceDictionary sans x:Class (ex: Resources/Styles/Converters.xaml) sont
            // analyses a l'execution, et MAUI ne collecte les [XmlnsDefinition] que sur les
            // assemblies deja chargees : sans ca, l'URI .../maui/converters est introuvable.
            _ = typeof(SqlToolManagerMaui.UI.Converters.BrushToColorConverter).Assembly;

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .UseFluentMauiIcons()
                .UseMauiCommunityToolkit(options => options.SetShouldEnableSnackbarOnWindows(true))
                .UseMauiCommunityToolkitMarkup();

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
