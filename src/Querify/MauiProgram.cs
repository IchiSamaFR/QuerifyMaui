using CodeEdit.Maui;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Mvvm.Messaging;
using Fluentia.Extensions;
using LiveChartsCore.SkiaSharpView.Maui;
using MauiIcons.Core;
using MauiIcons.Fluent;
using MauiIcons.Fluent.Filled;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Querify.Services.Diagnostics;
using Querify.UI.Extensions;

namespace Querify;

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
            .UseMauiCommunityToolkitMarkup()
            .UseFluentia();

        builder.Services.AddApplicationServices();

        MauiApp app = builder.Build();
        // Locator statique, pour le peu de code qui ne peut pas recevoir ses dépendances.
        Ioc.Initialize(app.Services);

        // Journal abonne des le demarrage : aucune erreur ne doit lui echapper.
        app.Services.GetRequiredService<FileLogWriter>();
        LogUnhandledExceptions();


        return app;
    }

    /// <summary>Exceptions echappees aux try/catch : journalisees avant un eventuel arret du processus.</summary>
    private static void LogUnhandledExceptions()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                WeakReferenceMessenger.Default.LogError(exception, $"Exception non gérée (arrêt : {e.IsTerminating}).");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WeakReferenceMessenger.Default.LogError(e.Exception, "Tâche en échec jamais attendue.");
            e.SetObserved();
        };
    }
}
