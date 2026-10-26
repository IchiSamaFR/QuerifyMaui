using CommunityToolkit.Mvvm.Messaging;
using Querify.UI.Extensions;

namespace Querify.WinUI;

/// <summary>
/// Rend la fermeture de la fenetre annulable : MAUI ne leve que Destroying, trop tard pour
/// demander d'enregistrer. On passe par l'AppWindow WinUI, dont Closing accepte Cancel.
/// </summary>
public static class WindowCloseGuard
{
    public static void Attach(Microsoft.Maui.Controls.Window window, Func<Task<bool>> canCloseAsync)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(canCloseAsync);

        Microsoft.UI.Xaml.Window? attached = null;
        var isConfirmed = false;

        window.HandlerChanged += (_, _) =>
        {
            if (attached is not null || window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
                return;

            attached = nativeWindow;
            nativeWindow.AppWindow.Closing += async (_, args) =>
            {
                if (isConfirmed)
                    return;

                // Annule avant tout await : la reponse de l'utilisateur arrive plus tard.
                args.Cancel = true;

                try
                {
                    if (!await canCloseAsync())
                        return;
                }
                catch (Exception ex)
                {
                    // Une question qui echoue ne doit pas empecher de quitter l'application.
                    WeakReferenceMessenger.Default.LogError(ex, "Confirmation de fermeture en échec.");
                }

                isConfirmed = true;
                nativeWindow.Close();
            };
        };
    }
}
