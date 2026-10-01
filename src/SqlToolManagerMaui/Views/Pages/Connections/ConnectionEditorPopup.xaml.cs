using CommunityToolkit.Maui.Views;
using SqlToolManagerMaui.ViewModels.Pages.Connections;

namespace SqlToolManagerMaui.Views.Pages.Connections;

/// <summary>Popup "Se connecter" : le VM la ferme via IPopupService en renvoyant l'Id enregistre (Guid?).</summary>
public partial class ConnectionEditorPopup : Popup
{
    public ConnectionEditorPopup(ConnectionEditorVM viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
