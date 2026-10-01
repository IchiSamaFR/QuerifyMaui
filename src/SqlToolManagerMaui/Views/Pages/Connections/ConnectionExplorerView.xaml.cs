using SqlToolManagerMaui.UI.ViewModels;
using SqlToolManagerMaui.UI.Views;
using SqlToolManagerMaui.ViewModels.Pages.Connections;

namespace SqlToolManagerMaui.Views.Pages.Connections;

public partial class ConnectionExplorerView : AbstractContentView
{
	public ConnectionExplorerView(ConnectionExplorerVM viewModel)
		: base(viewModel)
    {
		InitializeComponent();
	}
}