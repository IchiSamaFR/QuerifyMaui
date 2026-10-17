using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.UI.Views;
using SqlToolManagerMaui.ViewModels.Pages;
using SqlToolManagerMaui.ViewModels.Pages.Queries;
using SqlToolManagerMaui.Views.Pages.Connections;

namespace SqlToolManagerMaui.Views.Pages;

/// <summary>
/// Editeur de texte / SQL multi-onglets. Le contenu, les options d'affichage, la position
/// du curseur et la coloration passent par des bindings sur le controle CodeEditor :
/// la page n'a plus de logique propre.
/// </summary>
[AutoRegister]
public partial class QueryPage : AbstractPage
{
    public static readonly string PAGE_ROUTE = nameof(QueryPage);
    protected override string PageRoute => PAGE_ROUTE;

    public QueryPage(QueryVM viewModel, ConnectionExplorerView connectionExplorerView)
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
        if (ContextViewModel is QueryVM vm)
        {
            foreach (var item in vm.MainMenuBar.MenuItems)
                MenuBarItems.Add(item);
        }
    }
}
