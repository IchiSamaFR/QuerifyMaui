using Querify.Core.Models.Dashboards;
using Fluentia.Controls;

namespace Querify.Views.Pages.Documents;

/// <summary>Choisit la vue d'un onglet selon le type de sa session ; une seule vue par type, partagee par les onglets.</summary>
public class DocumentViewSelector : TabViewContentSelector
{
    public View? CodeEditorView { get; set; }

    public View? DashboardView { get; set; }

    public override View SelectView(object item)
        => (item is DashboardSession ? DashboardView : CodeEditorView)
           ?? throw new InvalidOperationException($"Vue non renseignee pour {item.GetType().Name}.");
}
