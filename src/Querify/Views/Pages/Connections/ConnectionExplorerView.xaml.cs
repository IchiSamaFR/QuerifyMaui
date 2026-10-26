using CommunityToolkit.Maui;
using Querify.Core.Ioc;
using Querify.UI.Views;
using Querify.ViewModels.Pages.Connections;
using System.Windows.Input;

namespace Querify.Views.Pages.Connections;

/// <summary>Explorateur d'objets (volet gauche facon SSMS).</summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class ConnectionExplorerView : AbstractContentView
{
    /// <summary>Ferme le volet (le hote en garde l'etat).</summary>
    [BindableProperty]
    public partial ICommand? CloseCommand { get; set; }

    /// <summary>VM type, pour les bindings des DataTemplate vers les commandes de l'explorateur.</summary>
    public ConnectionExplorerVM ViewModel { get; }

    public ConnectionExplorerView(ConnectionExplorerVM viewModel)
        : base(viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}