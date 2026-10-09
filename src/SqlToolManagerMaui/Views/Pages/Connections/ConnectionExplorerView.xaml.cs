using SqlToolManagerMaui.UI.Views;
using SqlToolManagerMaui.ViewModels.Pages.Connections;

namespace SqlToolManagerMaui.Views.Pages.Connections;

/// <summary>Explorateur d'objets (volet gauche facon SSMS).</summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class ConnectionExplorerView : AbstractContentView
{
    /// <summary>VM type, pour les bindings des DataTemplate vers les commandes de l'explorateur.</summary>
    public ConnectionExplorerVM ViewModel { get; }

    public ConnectionExplorerView(ConnectionExplorerVM viewModel)
        : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}