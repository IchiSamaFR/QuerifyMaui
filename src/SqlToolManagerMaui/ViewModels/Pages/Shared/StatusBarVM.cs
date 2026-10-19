using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Ioc;

namespace SqlToolManagerMaui.ViewModels.Pages.Shared;

/// <summary>Barre d'etat de la page principale, partagee par tous les onglets.</summary>
[AutoRegister]
public partial class StatusBarVM : ObservableObject
{
    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsError { get; set; }

    public void Set(string message, bool isError = false)
    {
        Message = message;
        IsError = isError;
    }

    public void Clear() => Set(string.Empty);
}
