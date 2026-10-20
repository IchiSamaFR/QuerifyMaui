using SqlToolManagerMaui.Core.Ioc;

namespace SqlToolManagerMaui;

/// <summary>Une seule page (MainPage) : pas de volet de navigation, l'explorateur d'objets vit dans la page.</summary>
[AutoRegister]
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

#if WINDOWS
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.NavigationView navigationView)
        {
            // Hauteur de l'en-tête (zone du titre / barre du haut)
            navigationView.PaneTitle = string.Empty;
            navigationView.Loaded += (_, _) =>
            {
                if (navigationView.Header is Microsoft.UI.Xaml.FrameworkElement header)
                    header.Height = 40;
            };
        }
    }
#endif
}
