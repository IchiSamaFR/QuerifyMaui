using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Models;
using SqlToolManagerMaui.Services;
using SqlToolManagerMaui.UI.Converters;
using SqlToolManagerMaui.UI.Extensions;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>
/// Gestion des connexions enregistrees : liste a gauche, formulaire a droite.
/// Le formulaire travaille sur des champs plats (Edit*) et non sur l'element
/// selectionne, afin qu'un abandon (Annuler) n'ait rien modifie dans la liste.
/// </summary>
public partial class ConnectionsVM : AbstractVM
{
    private readonly IConnectionStore _store;
    private readonly IConnectionTesterResolver _testerResolver;

    [ObservableProperty]
    public partial List<DatabaseConnectionInfo> Connections { get; set; } = new();

    [ObservableProperty]
    public partial DatabaseConnectionInfo? EditConnection { get; set; }

    /// <summary>Element selectionne dans la liste. Le setter charge le formulaire.</summary>
    [ObservableProperty]
    public partial DatabaseConnectionInfo? SelectedConnection { get; set; }

    /// <summary>Chaine complete (mot de passe inclus) telle que lue dans le coffre.</summary>
    private string _cachedConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Chaine affichee dans le formulaire. A la selection d'une connexion, le mot de passe
    /// y est masque ; il n'est restitue qu'au moment d'enregistrer ou de tester, et
    /// seulement si l'utilisateur a laisse le masque en place (voir
    /// <see cref="ResolveConnectionString"/>).
    /// </summary>
    [ObservableProperty]
    public partial string EditConnectionString { get; set; } = string.Empty;


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

        EditConnection = info.Clone();

        _cachedConnectionString = await _store.GetConnectionStringAsync(info.Id, LifetimeToken) ?? string.Empty;
        EditConnectionString = ConnectionStringMaskerConverter.Mask(_cachedConnectionString);

        if (string.IsNullOrEmpty(_cachedConnectionString))
            SetStatus("Chaine introuvable dans le coffre : saisissez-la de nouveau.", isError: true);
    }

    private void ClearSelection()
    {
        _suppressSelectionHandling = true;
        if (SelectedConnection is not null)
            SelectedConnection = null;
        if (EditConnection is not null)
            EditConnection = null;
        _suppressSelectionHandling = false;
    }

    [RelayCommand]
    private void New()
    {
        ClearSelection();
        EditConnection = new()
        {
            Provider = DatabaseProvider.SqlServer
        };
        _cachedConnectionString = string.Empty;
        EditConnectionString = _cachedConnectionString;

        SetStatus(string.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseForm();

    [RelayCommand]
    private Task SaveAsync() => RunAsBusyAsync(SaveBusyAsync, skipIfBusy: true);

    private async Task SaveBusyAsync()
    {
        if (string.IsNullOrEmpty(EditConnection?.Name))
        {
            SetStatus("Donnez un nom a la connexion.", isError: true);
            return;
        }

        var connectionString = ResolveConnectionString();
        var error = _testerResolver.Resolve(EditConnection?.Provider ?? DatabaseProvider.SqlServer)
            .Validate(connectionString, EditConnection);
        if (error is not null)
        {
            SetStatus(error, isError: true);
            return;
        }

        var cachedConnection = EditConnection;
        if (cachedConnection.IsNew)
            cachedConnection.Id = Guid.NewGuid();
        await _store.SaveAsync(cachedConnection, connectionString, LifetimeToken);

        // Rechargement complet : le store est seul maitre du tri et des dates.
        await LoadDatasBusyAsync();

        CloseForm();

        // Reouverture explicite (et non via le setter) : le handler de selection est
        // asynchrone et ecraserait le message de confirmation pose juste apres.
        var saved = Connections.FirstOrDefault(c => c.Id == cachedConnection?.Id);
        if (saved is not null)
        {
            _suppressSelectionHandling = true;
            SelectedConnection = saved;
            _suppressSelectionHandling = false;

            await OpenExistingAsync(saved);
        }

        SetStatus($"Connexion \"{EditConnection?.Name}\" enregistree.");
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
        var connectionString = ResolveConnectionString();
        var tester = _testerResolver.Resolve(EditConnection?.Provider ?? DatabaseProvider.SqlServer);

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

    /// <summary>
    /// Chaine reellement utilisee : tant que le masque est en place, il vaut
    /// "mot de passe inchange" et l'ancienne valeur est reinjectee. Un mot de passe
    /// ressaisi remplace le masque, donc gagne.
    /// </summary>
    private string ResolveConnectionString()
        => ConnectionStringMaskerConverter.Unmask(EditConnectionString?.Trim() ?? string.Empty, _cachedConnectionString);

    private void CloseForm()
    {
        ClearSelection();

        EditConnectionString = string.Empty;
        _cachedConnectionString = string.Empty;
        SetStatus(string.Empty);
    }

    private void SetStatus(string message, bool isError = false)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }
}
