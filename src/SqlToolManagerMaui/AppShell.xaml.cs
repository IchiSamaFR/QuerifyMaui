using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.ViewModels;
using SqlToolManagerMaui.Views.Pages.Connections;

namespace SqlToolManagerMaui
{
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

        //#if WINDOWS
        //        /// <summary>
        //        /// Sous Windows, le Shell est rendu par un NavigationView WinUI qui affiche
        //        /// son propre bouton hamburger (le carre en haut a gauche), son bouton retour
        //        /// et un item "Settings". Le rail gere deja tout cela : on les masque.
        //        /// </summary>
        //        protected override void OnHandlerChanged()
        //        {
        //            base.OnHandlerChanged();

        //            if (Handler?.PlatformView is Microsoft.UI.Xaml.Controls.NavigationView navigationView)
        //            {
        //                navigationView.IsPaneToggleButtonVisible = false;
        //                navigationView.IsSettingsVisible = false;
        //                navigationView.IsBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible.Collapsed;
        //                navigationView.IsPaneOpen = true;
        //                navigationView.PaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Left;
        //            }
        //        }
        //#endif
    }
}
