using CodeEdit.Maui;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Mvvm.Messaging;
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
    /// <summary>
    /// Anciens dossiers de donnees (editeur\ApplicationId, voir csproj et Package.appxmanifest), du plus recent au plus ancien :
    /// le premier trouve est repris.
    /// </summary>
    private static readonly string[] PreviousDataRoots =
    [
        @"User Name\fr.ichisamafr.querify",
        @"User Name\com.companyname.sqltoolmanagermaui",
    ];

    public static MauiApp CreateMauiApp()
    {
#if WINDOWS
        // Avant tout acces au dossier de donnees (stores, journal).
        var migrationError = WinUI.AppDataMigration.MigrateFrom(PreviousDataRoots);
#endif

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

        MauiApp app = builder.Build();
        // Locator statique, pour le peu de code qui ne peut pas recevoir ses dépendances.
        Ioc.Initialize(app.Services);

        // Journal abonne des le demarrage : aucune erreur ne doit lui echapper.
        app.Services.GetRequiredService<FileLogWriter>();
        LogUnhandledExceptions();

#if WINDOWS
        if (migrationError is not null)
            WeakReferenceMessenger.Default.LogError(migrationError, $"Données de l'ancienne version non reprises : {migrationError.Message}");
#endif

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
