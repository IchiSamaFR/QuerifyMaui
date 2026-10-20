// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.UI.Extensions;

namespace SqlToolManagerMaui.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        // Les exceptions du thread UI passent par WinUI, pas par AppDomain.UnhandledException.
        UnhandledException += (_, e) => WeakReferenceMessenger.Default.LogError(e.Exception, "Exception non gérée sur le thread UI.");
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
