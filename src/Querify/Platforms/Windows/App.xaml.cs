using CommunityToolkit.Mvvm.Messaging;
using Querify.UI.Extensions;

namespace Querify.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();

        // Les exceptions du thread UI passent par WinUI, pas par AppDomain.UnhandledException.
        UnhandledException += OnUnhandledException;
    }

    /// <summary>
    /// Erreur d'un gestionnaire d'interface (clic, binding...) : signalee sans fermer l'application,
    /// sinon les onglets non enregistres seraient perdus.
    /// </summary>
    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        WeakReferenceMessenger.Default.LogError(e.Exception, $"Erreur inattendue : {e.Exception.Message}");
        e.Handled = true;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
