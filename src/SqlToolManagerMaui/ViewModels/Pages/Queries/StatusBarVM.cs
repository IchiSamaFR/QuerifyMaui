using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Ioc;

namespace SqlToolManagerMaui.ViewModels.Pages.Queries;

/// <summary>Barre d'etat de la page Requete, partagee par ses sous-VM.</summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
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
