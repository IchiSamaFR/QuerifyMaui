using CommunityToolkit.Maui.Views;
using SqlToolManagerMaui.ViewModels.Pages.Dialogs;

namespace SqlToolManagerMaui.Views.Pages.Dialogs;

/// <summary>Boite de dialogue generique : le VM la ferme via IPopupService en renvoyant le bouton choisi.</summary>
public partial class MessageDialogPopup : Popup
{
    public MessageDialogPopup(MessageDialogVM viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
