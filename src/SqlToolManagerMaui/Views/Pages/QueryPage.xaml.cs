using SqlToolManagerMaui.ViewModels.Pages;

namespace SqlToolManagerMaui.Views.Pages;

/// <summary>
/// Editeur de texte / SQL multi-onglets. Le contenu, les options d'affichage, la position
/// du curseur et la coloration passent par des bindings sur le controle CodeEditor :
/// la page n'a plus de logique propre.
/// </summary>
public partial class QueryPage : AbstractPage
{
    public static readonly string PAGE_ROUTE = nameof(QueryPage);
    protected override string PageRoute => PAGE_ROUTE;

    /// <summary>Acces type au VM, utilise par les bindings des DataTemplate.</summary>
    public QueryVM ViewModel { get; }

    public QueryPage(QueryVM viewModel) : base(viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();
    }
}
