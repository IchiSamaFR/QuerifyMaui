using SqlToolManagerMaui.ViewModels.Pages;

namespace SqlToolManagerMaui.Views.Pages;

public partial class QueryPage : AbstractPage
{
    public static readonly string PAGE_ROUTE = nameof(QueryPage);
    protected override string PageRoute => PAGE_ROUTE;

    public QueryPage(QueryVM viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}