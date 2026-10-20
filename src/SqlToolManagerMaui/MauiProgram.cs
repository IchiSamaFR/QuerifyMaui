using CodeEdit.Maui;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using LiveChartsCore.SkiaSharpView.Maui;
using MauiIcons.Core;
using MauiIcons.Fluent;
using MauiIcons.Fluent.Filled;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace SqlToolManagerMaui;

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
            .UseMauiIconsCore()
            .UseFluentMauiIcons()
            .UseFluentFilledMauiIcons()
            .UseCodeEditor()
            .UseSkiaSharp()
            .UseLiveCharts()
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
