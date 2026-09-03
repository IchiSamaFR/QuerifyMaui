using SqlToolManagerMaui.ViewModels.Pages;
using SqlToolManagerMaui.Views;

namespace SqlToolManagerMaui.Views.Pages;

public partial class MainPage : AbstractPage
{
    public static readonly string PAGE_ROUTE = nameof(MainPage);
    protected override string PageRoute => PAGE_ROUTE;

    public MainPage(MainVM viewModel) : base(viewModel)
    {
        InitializeComponent();
    }
}
