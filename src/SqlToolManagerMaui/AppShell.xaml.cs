using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.ViewModels;
using SqlToolManagerMaui.Views.Pages.Connections;

namespace SqlToolManagerMaui;

[AutoRegister]
public partial class AppShell : Shell
{
    public ShellVM ContextViewModel { get; }

    public AppShell(ConnectionExplorerView flyoutContent, ShellVM viewModel)
    {
        FlyoutContent = flyoutContent;
        ContextViewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        // Garde la selection du menu alignee sur la page reellement affichee.
        var route = CurrentState?.Location?.OriginalString?.Trim('/');
        if (!string.IsNullOrEmpty(route))
            ContextViewModel.CurrentRoute = route.Split('/').Last();
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
