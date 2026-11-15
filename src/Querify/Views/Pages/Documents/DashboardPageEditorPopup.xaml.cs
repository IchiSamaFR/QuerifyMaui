using CommunityToolkit.Maui.Views;
using Querify.ViewModels.Pages.Documents;

namespace Querify.Views.Pages.Documents;

/// <summary>Popup "Options de la page" : le VM la ferme via IPopupService en renvoyant la saisie.</summary>
public partial class DashboardPageEditorPopup : Popup
{
    public DashboardPageEditorPopup(DashboardPageEditorVM viewModel)
    {
        // Contexte pose apres la construction, comme pour les pages : les bindings s'appliquent sur l'arbre complet.
        InitializeComponent();
        BindingContext = viewModel;
    }
}
