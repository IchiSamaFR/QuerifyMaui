using CommunityToolkit.Maui.Extensions;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Dashboards;
using SqlToolManagerMaui.Services.Dialogs;
using SqlToolManagerMaui.ViewModels.Pages.Documents;
using SqlToolManagerMaui.Views.Pages.Documents;

namespace SqlToolManagerMaui.Services.Dashboards;

/// <inheritdoc cref="IDashboardParameterDialogService"/>
[AutoRegister(As = typeof(IDashboardParameterDialogService))]
public sealed class DashboardParameterDialogService : IDashboardParameterDialogService
{
    private readonly IServiceProvider _services;

    public DashboardParameterDialogService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<DashboardParameterDefinition?> EditAsync(DashboardParameterDefinition? parameter, IReadOnlyCollection<string> usedNames, CancellationToken cancellationToken = default)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null)
            return null;

        // VM transient : chaque ouverture repart d'un formulaire vierge.
        var viewModel = _services.GetRequiredService<DashboardParameterEditorVM>();
        viewModel.Initialize(parameter, usedNames);

        var result = await page.ShowPopupAsync<DashboardParameterDefinition?>(new DashboardParameterEditorPopup(viewModel), FluentPopupOptions.Create(), cancellationToken);
        return result.Result;
    }
}
