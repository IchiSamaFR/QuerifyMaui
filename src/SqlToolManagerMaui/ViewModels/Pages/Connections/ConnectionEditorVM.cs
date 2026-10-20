using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Databases.Connections;
using SqlToolManagerMaui.Databases.Enums;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.UI.Extensions;
using SqlToolManagerMaui.UI.ViewModels;

namespace SqlToolManagerMaui.ViewModels.Pages.Connections;

/// <summary>
/// Popup "Se connecter" : creation, modification et test d'une connexion.
/// Le formulaire travaille sur un <see cref="DatabaseConnectionDraft"/> (clone du descripteur
/// + chaine de connexion), afin qu'un abandon n'ait rien modifie et que le secret ne touche
/// jamais le type serialise.
/// </summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class ConnectionEditorVM : AbstractVM
{
    private readonly IConnectionStore _store;
    private readonly IConnectionTesterResolver _testerResolver;
    private readonly IPopupService _popupService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial DatabaseConnectionDraft? Draft { get; set; }

    public string Title => Draft?.Info.IsEditing == true ? "Modifier la connexion" : "Se connecter";

    /// <summary>Message de validation ou de test, affiche sous le formulaire.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Pilote la couleur du message : vrai pour une erreur.</summary>
    [ObservableProperty]
    public partial bool IsStatusError { get; set; }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public ConnectionEditorVM(IConnectionStore store, IConnectionTesterResolver testerResolver, IPopupService popupService)
    {
        _store = store;
        _testerResolver = testerResolver;
        _popupService = popupService;
    }

    /// <summary>Prepare le formulaire avant l'ouverture de la popup.</summary>
    public async Task InitializeAsync(DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default)
    {
        SetStatus(string.Empty);

        if (connection is null)
        {
            Draft = DatabaseConnectionDraft.ForNew();
            return;
        }

        var stored = await _store.GetConnectionStringAsync(connection.Id, cancellationToken);
        Draft = DatabaseConnectionDraft.ForExisting(connection, stored);

        if (!Draft.HasStoredConnectionString)
            SetStatus("Chaine introuvable dans le coffre : saisissez-la de nouveau.", isError: true);
    }

    [RelayCommand]
    private Task CancelAsync() => CloseAsync(null);

    [RelayCommand]
    private Task SaveAsync() => RunAsBusyAsync(SaveBusyAsync, skipIfBusy: true);

    private async Task SaveBusyAsync()
    {
        var info = Draft?.Info;
        if (string.IsNullOrEmpty(info?.Name))
        {
            SetStatus("Donnez un nom a la connexion.", isError: true);
            return;
        }

        var connectionString = Draft!.ResolveConnectionString();
        var error = _testerResolver.Resolve(info.Provider).Validate(connectionString, info);
        if (error is not null)
        {
            SetStatus(error, isError: true);
            return;
        }

        if (info.IsNew)
            info.Id = Guid.NewGuid();

        try
        {
            await _store.SaveAsync(info, connectionString, LifetimeToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Affiche aussi dans la popup, restee ouverte : la barre d'etat de la page est masquee dessous.
            WeakReferenceMessenger.Default.LogError(ex, $"Connexion non enregistrée : {ex.Message}");
            SetStatus($"Enregistrement impossible : {ex.Message}", isError: true);
            return;
        }

        await CloseAsync(info.Id);
    }

    [RelayCommand]
    private Task TestAsync() => RunAsBusyAsync(TestBusyAsync, skipIfBusy: true);

    private async Task TestBusyAsync()
    {
        var connectionString = Draft?.ResolveConnectionString() ?? string.Empty;
        var tester = _testerResolver.Resolve(Draft?.Info.Provider ?? DatabaseProvider.SqlServer);

        var error = tester.Validate(connectionString, new DatabaseConnectionInfo());
        if (error is not null)
        {
            SetStatus(error, isError: true);
            return;
        }

        SetStatus("Test en cours...");
        var result = await tester.TestAsync(connectionString, LifetimeToken);

        SetStatus(
            $"{result.Message} ({result.Elapsed.TotalMilliseconds:F0} ms)",
            isError: !result.Success);
    }

    private async Task CloseAsync(Guid? savedId)
    {
        if (Shell.Current is { } shell)
            await _popupService.ClosePopupAsync(shell, savedId);
    }

    private void SetStatus(string message, bool isError = false)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }
}
