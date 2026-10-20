using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using SqlToolManagerMaui.UI.Models.Messagings;

namespace SqlToolManagerMaui.UI.Extensions;

/// <summary>Journalisation de l'application : un seul point d'entree, le messenger (<see cref="WeakLog"/>).</summary>
public static class WeakReferenceMessengerExtensions
{
    public static void RegisterLogHandler(this WeakReferenceMessenger messenger, object recipient, Action<WeakLog> action)
    {
        if (messenger is null || recipient is null || action is null)
            return;
        messenger.Register<WeakLog>(recipient, (r, message) => action(message));
    }

    public static void LogError<TEx>(this WeakReferenceMessenger messenger, TEx exception, string? message = null)
        where TEx : Exception
    {
        if (messenger is null)
            return;

        messenger.Send(new WeakLog(message, exception, LogLevel.Error));
    }

    public static void LogInfo(this WeakReferenceMessenger messenger, string message)
    {
        if (messenger is null)
            return;

        messenger.Send(new WeakLog(message, null, LogLevel.Information));
    }
}
