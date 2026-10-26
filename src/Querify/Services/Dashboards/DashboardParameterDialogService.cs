using CommunityToolkit.Maui.Extensions;
using Querify.Core.Ioc;
using Querify.Core.Models.Dashboards;
using Querify.Services.Dialogs;
using Querify.ViewModels.Pages.Documents;
using Querify.Views.Pages.Documents;

namespace Querify.Services.Dashboards;

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
