using SqlToolManagerMaui.ViewModels.Pages.Documents;

namespace SqlToolManagerMaui.Views.Pages.Documents;

/// <summary>Choisit la vue d'un onglet selon le type de son ViewModel.</summary>
public class DocumentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CodeEditorTemplate { get; set; }

    public DataTemplate? DashboardTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is DashboardVM)
            return DashboardTemplate ?? throw new InvalidOperationException($"Gabarit non renseigne pour {item.GetType().Name}.");
        if (item is CodeEditorVM)
            return CodeEditorTemplate ?? throw new InvalidOperationException($"Gabarit non renseigne pour {item.GetType().Name}.");
        
        throw new InvalidOperationException($"Gabarit non renseigne pour {item.GetType().Name}.");
    }
}
