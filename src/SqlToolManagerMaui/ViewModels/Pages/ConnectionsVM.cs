using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Models;
using SqlToolManagerMaui.Models.Databases;
using SqlToolManagerMaui.Services;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.UI.Extensions;
using SqlToolManagerMaui.UI.ViewModels;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>
/// Gestion des connexions enregistrees : liste a gauche, formulaire a droite.
/// Le formulaire travaille sur un <see cref="DatabaseConnectionDraft"/> (clone du descripteur
/// + chaine de connexion), afin qu'un abandon (Annuler) n'ait rien modifie dans la liste
/// et que le secret ne touche jamais le type serialise.
/// </summary>
public partial class ConnectionsVM : AbstractVM
{
    private readonly IConnectionStore _store;
    private readonly IConnectionTesterResolver _testerResolver;

    [ObservableProperty]
    public partial List<DatabaseConnectionInfo> Connections { get; set; } = new();

    /// <summary>Formulaire ouvert, ou null quand aucune connexion n'est en cours d'edition.</summary>
    [ObservableProperty]
    public partial DatabaseConnectionDraft? Draft { get; set; }

    /// <summary>Element selectionne dans la liste. Le setter charge le formulaire.</summary>
    [ObservableProperty]
    public partial DatabaseConnectionInfo? SelectedConnection { get; set; }

    /// <summary>Message de validation ou de test, affiche sous le formulaire.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Pilote la couleur du message : vrai pour une erreur.</summary>
    [ObservableProperty]
    public partial bool IsStatusError { get; set; }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    /// <summary>Neutralise le handler de selection quand le VM repositionne lui-meme la liste.</summary>
    private bool _suppressSelectionHandling;

    public ConnectionsVM(IConnectionStore store, IConnectionTesterResolver testerResolver)
    {
        _store = store;
        _testerResolver = testerResolver;
    }

    protected override async Task LoadDatasBusyAsync()
    {
        var items = await _store.GetAllAsync(LifetimeToken);
        Connections = new(items);
    }

    partial void OnSelectedConnectionChanged(DatabaseConnectionInfo? value)
    {
        if (_suppressSelectionHandling)
            return;

        if (value is null)
        {
            CloseForm();
            return;
        }

        StartAsBusy(() => OpenExistingAsync(value));
    }

    private async Task OpenExistingAsync(DatabaseConnectionInfo info)
    {
        SetStatus(string.Empty);

        Draft = DatabaseConnectionDraft.ForExisting(info, await _store.GetConnectionStringAsync(info.Id, LifetimeToken));

        if (!Draft.HasStoredConnectionString)
            SetStatus("Chaine introuvable dans le coffre : saisissez-la de nouveau.", isError: true);
    }

    private void ClearSelection()
    {
        _suppressSelectionHandling = true;
        if (SelectedConnection is not null)
            SelectedConnection = null;
        if (Draft is not null)
            Draft = null;
        _suppressSelectionHandling = false;
    }

    [RelayCommand]
    private void New()
    {
        ClearSelection();
        Draft = DatabaseConnectionDraft.ForNew();

        SetStatus(string.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseForm();

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
        await _store.SaveAsync(info, connectionString, LifetimeToken);

        // Rechargement complet : le store est seul maitre du tri et des dates.
        await LoadDatasBusyAsync();

        CloseForm();

        // Reouverture explicite (et non via le setter) : le handler de selection est
        // asynchrone et ecraserait le message de confirmation pose juste apres.
        var saved = Connections.FirstOrDefault(c => c.Id == info.Id);
        if (saved is not null)
        {
            _suppressSelectionHandling = true;
            SelectedConnection = saved;
            _suppressSelectionHandling = false;

            await OpenExistingAsync(saved);
        }

        SetStatus($"Connexion \"{info.Name}\" enregistree.");
    }

    [RelayCommand]
    private Task DeleteAsync(DatabaseConnectionInfo? info) => RunAsBusyAsync(() => DeleteBusyAsync(info), skipIfBusy: true);

    private async Task DeleteBusyAsync(DatabaseConnectionInfo? info)
    {
        if (info is null)
            return;

        var page = Shell.Current?.CurrentPage;
        if (page is not null)
        {
            var confirmed = await page.DisplayAlertAsync(
                "Supprimer la connexion",
                $"Supprimer definitivement \"{info.Name}\" ? La chaine de connexion sera effacee du coffre.",
                "Supprimer",
                "Annuler");

            if (!confirmed)
                return;
        }

        await _store.DeleteAsync(info.Id, LifetimeToken);
        await LoadDatasBusyAsync();

        CloseForm();
        SetStatus($"Connexion \"{info.Name}\" supprimee.");
    }

    [RelayCommand]
    private Task TestAsync() => RunAsBusyAsync(TestBusyAsync, skipIfBusy: true);

    private async Task TestBusyAsync()
    {
        var connectionString = Draft?.ResolveConnectionString() ?? string.Empty;
        var tester = _testerResolver.Resolve(Draft?.Info.Provider ?? DatabaseProvider.SqlServer);

        var probe = new DatabaseConnectionInfo();
        var error = tester.Validate(connectionString, probe);
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

    private void CloseForm()
    {
        ClearSelection();
        SetStatus(string.Empty);
    }

    private void SetStatus(string message, bool isError = false)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }
}
