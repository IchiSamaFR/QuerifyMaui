using CommunityToolkit.Maui.Extensions;
using SqlToolManagerMaui.Core.Enums.Dialogs;
using SqlToolManagerMaui.Core.Enums.Editors;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.ViewModels.Pages.Dialogs;
using SqlToolManagerMaui.Views.Pages.Dialogs;

namespace SqlToolManagerMaui.Services.Dialogs;

/// <inheritdoc cref="IDialogService"/>
[AutoRegister(As = typeof(IDialogService))]
public sealed class DialogService : IDialogService
{
    private const string SaveLabel = "Enregistrer";
    private const string DiscardLabel = "Ne pas enregistrer";
    private const string CancelLabel = "Annuler";

    private readonly IServiceProvider _services;

    public DialogService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<SaveChangesChoice> AskSaveChangesAsync(string documentName)
    {
        var result = await ShowAsync(
            "Modifications non enregistrées",
            $"Enregistrer les modifications de « {documentName} » ?",
            SaveLabel,
            DiscardLabel);

        return result switch
        {
            MessageDialogResult.Primary => SaveChangesChoice.Save,
            MessageDialogResult.Secondary => SaveChangesChoice.Discard,
            _ => SaveChangesChoice.Cancel,
        };
    }

    public async Task<bool> ConfirmAsync(string title, string message, string accept)
        => await ShowAsync(title, message, accept, secondaryButtonText: null) == MessageDialogResult.Primary;

    public async Task<string?> PromptAsync(string title, string message, string initialValue, string accept, Func<string, string?>? validate = null)
    {
        if (Shell.Current?.CurrentPage is not { } page)
            return null;

        var viewModel = _services.GetRequiredService<TextPromptVM>();
        viewModel.Initialize(title, message, initialValue, accept, validate);

        var result = await page.ShowPopupAsync<string?>(new TextPromptPopup(viewModel), FluentPopupOptions.Create());
        return result.WasDismissedByTappingOutsideOfPopup ? null : result.Result;
    }

    /// <summary>Sans page pour poser la question, rien n'est fait : <see cref="MessageDialogResult.None"/>.</summary>
    private async Task<MessageDialogResult> ShowAsync(string title, string message, string primaryButtonText, string? secondaryButtonText)
    {
        if (Shell.Current?.CurrentPage is not { } page)
            return MessageDialogResult.None;

        var viewModel = _services.GetRequiredService<MessageDialogVM>();
        viewModel.Initialize(title, message, primaryButtonText, secondaryButtonText, CancelLabel);

        var result = await page.ShowPopupAsync<MessageDialogResult>(new MessageDialogPopup(viewModel), FluentPopupOptions.Create());
        return result.WasDismissedByTappingOutsideOfPopup ? MessageDialogResult.None : result.Result;
    }
}
