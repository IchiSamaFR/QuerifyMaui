using Querify.Core.Ioc;
using Querify.UI.Views;
using Querify.ViewModels.Pages;
using Querify.Views.Pages.Connections;

namespace Querify.Views.Pages;

/// <summary>Page principale : explorateur d'objets et onglets, dont la vue est choisie par DocumentViewSelector.</summary>
[AutoRegister]
public partial class MainPage : AbstractPage
{
    public static readonly string PAGE_ROUTE = nameof(MainPage);
    protected override string PageRoute => PAGE_ROUTE;

    /// <summary>VM type pour les bindings des gabarits (menu contextuel des onglets).</summary>
    public MainVM ViewModel { get; }

    public MainPage(MainVM viewModel, ConnectionExplorerView connectionExplorerView)
    : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        ConnectionExplorerHost.Content = connectionExplorerView;

        // La vue de l'explorateur porte son propre BindingContext : la commande est liee a la source explicite.
        connectionExplorerView.SetBinding(ConnectionExplorerView.CloseCommandProperty,
            new Binding(nameof(MainVM.CloseObjectExplorerCommand), source: viewModel));
    }
}
