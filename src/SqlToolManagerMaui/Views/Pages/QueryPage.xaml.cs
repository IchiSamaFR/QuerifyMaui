using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.UI.Views;
using SqlToolManagerMaui.ViewModels.Pages;
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
    }
}
