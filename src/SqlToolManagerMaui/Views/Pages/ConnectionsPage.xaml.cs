using SqlToolManagerMaui.UI.Views;
using SqlToolManagerMaui.ViewModels.Pages;

namespace SqlToolManagerMaui.Views.Pages;

public partial class ConnectionsPage : AbstractPage
{
    public static readonly string PAGE_ROUTE = nameof(ConnectionsPage);
    protected override string PageRoute => PAGE_ROUTE;

    public ConnectionsPage(ConnectionsVM viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
