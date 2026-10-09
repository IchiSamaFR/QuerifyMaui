using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
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
        var result = await page.ShowPopupAsync<Guid?>(popup, CreateOptions(), cancellationToken);
        return result.Result;
    }

    // Habillage des surfaces flottantes WPF UI : coins 8 px, trait SurfaceStroke, voile SmokeFill.
    private static PopupOptions CreateOptions() => new()
    {
        CanBeDismissedByTappingOutsideOfPopup = false,
        PageOverlayColor = Resource("SmokeFill", Color.FromArgb("#4D000000")),
        Shape = new RoundRectangle
        {
            CornerRadius = 8,
            Stroke = Resource("SurfaceStrokeColorDefault", Color.FromArgb("#66757575")),
            StrokeThickness = 1,
        },
    };

    private static Color Resource(string key, Color fallback)
        => Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
}
