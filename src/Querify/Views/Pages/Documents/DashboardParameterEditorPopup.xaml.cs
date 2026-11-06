using CommunityToolkit.Maui.Views;
using Querify.ViewModels.Pages.Documents;

namespace Querify.Views.Pages.Documents;

/// <summary>Popup "Parametre" : le VM la ferme via IPopupService en renvoyant la definition saisie.</summary>
public partial class DashboardParameterEditorPopup : Popup
{
    public DashboardParameterEditorPopup(DashboardParameterEditorVM viewModel)
    {
        // Contexte pose apres la construction, comme pour les pages : les bindings s'appliquent sur l'arbre complet.
        InitializeComponent();
        BindingContext = viewModel;
    }
}
