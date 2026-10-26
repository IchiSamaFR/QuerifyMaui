using CommunityToolkit.Maui.Views;
using Querify.ViewModels.Pages.Connections;

namespace Querify.Views.Pages.Connections;

/// <summary>Popup "Se connecter" : le VM la ferme via IPopupService en renvoyant l'Id enregistre (Guid?).</summary>
public partial class ConnectionEditorPopup : Popup
{
    public ConnectionEditorPopup(ConnectionEditorVM viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
