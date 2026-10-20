using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.UI.Views;
using SqlToolManagerMaui.ViewModels.Pages;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using SqlToolManagerMaui.Views.Pages.Connections;

namespace SqlToolManagerMaui.Views.Pages;

/// <summary>Page principale : explorateur d'objets et onglets, dont la vue est choisie par DocumentViewSelector.</summary>
[AutoRegister]
public partial class MainPage : AbstractPage
{
    public static readonly string PAGE_ROUTE = nameof(MainPage);
    protected override string PageRoute => PAGE_ROUTE;

    public MainPage(MainVM viewModel, ConnectionExplorerView connectionExplorerView)
    : base(viewModel)
    {
        InitializeComponent();
        ConnectionExplorerHost.Content = connectionExplorerView;

        // La vue de l'explorateur porte son propre BindingContext : la commande est liee a la source explicite.
        connectionExplorerView.SetBinding(ConnectionExplorerView.CloseCommandProperty,
            new Binding(nameof(EditorPreferencesVM.CloseObjectExplorerCommand), source: viewModel.Preferences));
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        MenuBarItems.Clear();
        if (ContextViewModel is MainVM vm)
        {
            foreach (var item in vm.MainMenuBar.MenuItems)
                MenuBarItems.Add(item);
        }
    }
}
