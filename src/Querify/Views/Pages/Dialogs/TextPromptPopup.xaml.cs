using CommunityToolkit.Maui.Views;
using Querify.ViewModels.Pages.Dialogs;

namespace Querify.Views.Pages.Dialogs;

/// <summary>Saisie d'un texte : le VM la ferme via IPopupService en renvoyant la valeur, ou null si annulee.</summary>
public partial class TextPromptPopup : Popup
{
    public TextPromptPopup(TextPromptVM viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
