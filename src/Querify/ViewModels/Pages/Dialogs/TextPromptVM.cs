using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Querify.Core.Ioc;
using Querify.UI.ViewModels;

namespace Querify.ViewModels.Pages.Dialogs;

/// <summary>Saisie d'un texte (renommage...) : la popup reste ouverte tant que la validation le refuse.</summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class TextPromptVM : AbstractVM
{
    private readonly IPopupService _popupService;
    private Func<string, string?>? _validate;

    [ObservableProperty]
    public partial string Title { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Message { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AcceptButtonText { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? Error { get; private set; }

    public bool HasError => Error is not null;

    public TextPromptVM(IPopupService popupService)
    {
        _popupService = popupService;
    }

    /// <param name="validate">Message d'erreur pour une valeur refusee, null si elle convient.</param>
    public void Initialize(string title, string message, string value, string acceptButtonText, Func<string, string?>? validate)
    {
        Title = title;
        Message = message;
        Value = value;
        AcceptButtonText = acceptButtonText;
        _validate = validate;
        Error = null;
    }

    /// <summary>L'erreur disparait des que la saisie change.</summary>
    partial void OnValueChanged(string value) => Error = null;

    [RelayCommand]
    private Task AcceptAsync()
    {
        var value = Value.Trim();
        Error = _validate?.Invoke(value);
        return Error is null ? CloseWithAsync(value) : Task.CompletedTask;
    }

    [RelayCommand]
    private Task CancelAsync() => CloseWithAsync(null);

    private async Task CloseWithAsync(string? result)
    {
        if (Shell.Current is { } shell)
            await _popupService.ClosePopupAsync(shell, result);
    }
}
