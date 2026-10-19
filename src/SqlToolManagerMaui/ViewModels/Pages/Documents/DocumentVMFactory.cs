using SqlToolManagerMaui.Core.Enums.Dashboards;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors;

namespace SqlToolManagerMaui.ViewModels.Pages.Documents;

/// <summary>Cree le ViewModel d'un onglet : le document est fourni a l'execution, le reste par le conteneur.</summary>
[AutoRegister]
public class DocumentVMFactory
{
    private readonly IServiceProvider _services;

    public DocumentVMFactory(IServiceProvider services)
    {
        _services = services;
    }

    public CodeEditorVM CreateCodeEditor(EditorDocument document)
        => ActivatorUtilities.CreateInstance<CodeEditorVM>(_services, document);

    public DashboardVM CreateDashboard(EditorDocument document, DashboardMode mode = DashboardMode.View)
        => ActivatorUtilities.CreateInstance<DashboardVM>(_services, document, mode);
}
