using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.UI.Extensions;
using SqlToolManagerMaui.UI.Models.Messagings;

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

    public StatusBarVM()
    {
        // Erreurs journalisees (taches des VM, stores...) : affichees ici plutot que perdues.
        WeakReferenceMessenger.Default.RegisterLogHandler(this, HandleLog);
    }

    private void HandleLog(WeakLog log)
    {
        if (log.Level != LogLevel.Error)
            return;

        // L'erreur a pu etre publiee hors du thread UI.
        MainThread.BeginInvokeOnMainThread(() => Set($"Erreur : {log.Message ?? log.Exception?.Message}", isError: true));
    }

    public void Clear() => Set(string.Empty);
}
