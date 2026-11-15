using CommunityToolkit.Maui.Extensions;
using Querify.Core.Ioc;
using Querify.Core.Models.Dashboards;
using Querify.Services.Dialogs;
using Querify.ViewModels.Pages.Documents;
using Querify.Views.Pages.Documents;

namespace Querify.Services.Dashboards;

/// <inheritdoc cref="IDashboardPageDialogService"/>
[AutoRegister(As = typeof(IDashboardPageDialogService))]
public sealed class DashboardPageDialogService : IDashboardPageDialogService
{
    private readonly IServiceProvider _services;

    public DashboardPageDialogService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<DashboardPageEdit?> EditAsync(DashboardPageDefinition page, IReadOnlyCollection<string> usedIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (Shell.Current?.CurrentPage is not { } current)
            return null;

        // VM transient : chaque ouverture repart d'un formulaire vierge.
        var viewModel = _services.GetRequiredService<DashboardPageEditorVM>();
        viewModel.Initialize(page, usedIds);

        var result = await current.ShowPopupAsync<DashboardPageEdit?>(new DashboardPageEditorPopup(viewModel), FluentPopupOptions.Create(), cancellationToken);
        return result.Result;
    }
}
