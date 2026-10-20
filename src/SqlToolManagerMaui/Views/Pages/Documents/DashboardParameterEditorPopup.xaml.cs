using CommunityToolkit.Maui.Views;
using SqlToolManagerMaui.ViewModels.Pages.Documents;

namespace SqlToolManagerMaui.Views.Pages.Documents;

/// <summary>Popup "Parametre" : le VM la ferme via IPopupService en renvoyant la definition saisie.</summary>
public partial class DashboardParameterEditorPopup : Popup
{
    public DashboardParameterEditorPopup(DashboardParameterEditorVM viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
