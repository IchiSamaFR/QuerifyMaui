using LiveChartsCore.SkiaSharpView.Maui;
using Querify.Core.Models.Dashboards;
using Querify.Core.Serialization;
using Querify.Diagnostics;
using Fluentia.Controls;
using Querify.Views.Controls.Dashboards;
using SkiaSharp;
using System.Diagnostics;

namespace Querify.Services.Dashboards;

/// <summary>
/// Paie au demarrage, l'application deja affichee, ce que coutait la premiere ouverture d'un dashboard :
/// chargement des DLL LiveCharts / SkiaSharp et de la bibliotheque native Skia, lecture JSON, analyse XAML
/// et compilation JIT des vues de carte.
/// </summary>
public static class DashboardWarmup
{
    /// <summary>Laisse la fenetre finir de s'afficher (et les onglets restaures s'ouvrir) avant de prechauffer.</summary>
    private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(2);

    private static int _started;

    public static void Start(IDispatcher dispatcher)
    {
        if (Interlocked.Exchange(ref _started, 1) == 0)
            _ = RunAsync(dispatcher);
    }

    private static async Task RunAsync(IDispatcher dispatcher)
    {
        try
        {
            await Task.Delay(StartDelay);
            PerfTrace.Mark("Prechauffage : debut");

            // Hors thread UI : chargements et initialisations qui ne touchent a aucune vue.
            await Task.Run(() =>
            {
                using var _ = PerfTrace.Measure("Prechauffage : JSON, LiveCharts, Skia");
                DashboardSerializer.Deserialize(DashboardSerializer.Serialize(DashboardDefinition.CreateEmpty(null, null)));
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(LiveChartsCore.LiveCharts).TypeHandle);
                using var bitmap = new SKBitmap(1, 1);
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.Transparent);
            });

            await Task.Run(() =>
            {
                using var measure = PerfTrace.Measure("Prechauffage : JSON, LiveCharts, Skia");
                DashboardSerializer.Deserialize(DashboardSerializer.Serialize(DashboardDefinition.CreateEmpty(null, null)));
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(LiveChartsCore.LiveCharts).TypeHandle);
                using var bitmap = new SKBitmap(1, 1);
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.Transparent);
            });

            // Thread UI (creation de vues obligatoire ici) : des vues jamais affichees, donc sans vue native.
            await dispatcher.DispatchAsync(() =>
            {
                using var measure = PerfTrace.Measure("Prechauffage : vues de carte");
                _ = new DashboardCardView();
                _ = new DashboardChartView();
                _ = new CartesianChart();
                _ = new PieChart();
                _ = new DataGrid();
            });
        }
        catch (Exception ex)
        {
            // Simple optimisation : un echec ne doit jamais gener l'application.
            Debug.WriteLine($"[Perf] Prechauffage du dashboard en echec : {ex}");
        }
    }
}
