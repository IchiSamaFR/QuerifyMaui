using SqlToolManagerMaui.Core.Models.Dashboards;

namespace SqlToolManagerMaui.Views.Pages.Documents;

/// <summary>Choisit la vue d'un onglet selon le type de sa session.</summary>
public class DocumentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CodeEditorTemplate { get; set; }

    public DataTemplate? DashboardTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => (item is DashboardSession ? DashboardTemplate : CodeEditorTemplate)
           ?? throw new InvalidOperationException($"Gabarit non renseigne pour {item.GetType().Name}.");
}
