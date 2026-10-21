using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Dialogs;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.UI.ViewModels;

namespace SqlToolManagerMaui.ViewModels.Pages.Dialogs;

/// <summary>Boite de dialogue facon WPF UI : titre, message, bouton principal, secondaire facultatif et fermeture.</summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class MessageDialogVM : AbstractVM
{
    private readonly IPopupService _popupService;

    [ObservableProperty]
    public partial string Title { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string PrimaryButtonText { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSecondaryButton))]
    public partial string? SecondaryButtonText { get; private set; }

    [ObservableProperty]
    public partial string CloseButtonText { get; private set; } = string.Empty;

    public bool HasSecondaryButton => !string.IsNullOrEmpty(SecondaryButtonText);

    public MessageDialogVM(IPopupService popupService)
    {
        _popupService = popupService;
    }

    /// <summary>Prepare le contenu avant l'ouverture de la popup.</summary>
    public void Initialize(string title, string message, string primaryButtonText, string? secondaryButtonText, string closeButtonText)
    {
        Title = title;
        Message = message;
        PrimaryButtonText = primaryButtonText;
        SecondaryButtonText = secondaryButtonText;
        CloseButtonText = closeButtonText;
    }

    [RelayCommand]
    private Task PrimaryAsync() => CloseWithAsync(MessageDialogResult.Primary);

    [RelayCommand]
    private Task SecondaryAsync() => CloseWithAsync(MessageDialogResult.Secondary);

    [RelayCommand]
    private Task CloseAsync() => CloseWithAsync(MessageDialogResult.None);

    private async Task CloseWithAsync(MessageDialogResult result)
    {
        if (Shell.Current is { } shell)
            await _popupService.ClosePopupAsync(shell, result);
    }
}
