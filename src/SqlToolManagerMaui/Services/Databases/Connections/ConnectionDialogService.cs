using CommunityToolkit.Maui.Extensions;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Services.Dialogs;
using SqlToolManagerMaui.ViewModels.Pages.Connections;
using SqlToolManagerMaui.Views.Pages.Connections;

namespace SqlToolManagerMaui.Services.Databases.Connections;

/// <inheritdoc cref="IConnectionDialogService"/>
[AutoRegister(As = typeof(IConnectionDialogService))]
public sealed class ConnectionDialogService : IConnectionDialogService
{
    private readonly IServiceProvider _services;

    public ConnectionDialogService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<Guid?> EditAsync(DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null)
            return null;

        // VM transient : chaque ouverture repart d'un formulaire vierge.
        var viewModel = _services.GetRequiredService<ConnectionEditorVM>();
        await viewModel.InitializeAsync(connection, cancellationToken);

        var popup = new ConnectionEditorPopup(viewModel);
        var result = await page.ShowPopupAsync<Guid?>(popup, FluentPopupOptions.Create(), cancellationToken);
        return result.Result;
    }
}
