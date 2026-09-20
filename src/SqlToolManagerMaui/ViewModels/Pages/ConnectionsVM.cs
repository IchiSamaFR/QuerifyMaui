using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Models;
using SqlToolManagerMaui.Services;

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

    public ObservableCollection<DatabaseConnectionInfo> Connections { get; } = [];

    public bool HasConnections => Connections.Count > 0;

    /// <summary>Element selectionne dans la liste. Le setter charge le formulaire.</summary>
    [ObservableProperty]
    public partial DatabaseConnectionInfo? SelectedConnection { get; set; }

    /// <summary>Vrai des qu'un formulaire (creation ou edition) est ouvert.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormClosed))]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    public partial bool IsEditing { get; set; }

    public bool IsFormClosed => !IsEditing;

    /// <summary>Vrai pour une creation, faux pour la modification d'une connexion existante.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    public partial bool IsNew { get; set; }

    public string FormTitle => !IsEditing
        ? string.Empty
        : IsNew ? "Nouvelle connexion" : "Modifier la connexion";

    [ObservableProperty]
    public partial string EditName { get; set; } = string.Empty;

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

    /// <summary>Id en cours d'edition : nouveau Guid en creation, Id existant sinon.</summary>
    private Guid _editingId = Guid.Empty;

    private DatabaseProvider _editingProvider = DatabaseProvider.SqlServer;

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

        Connections.Clear();
        foreach (var item in items)
            Connections.Add(item);

        OnPropertyChanged(nameof(HasConnections));
    }

    partial void OnSelectedConnectionChanged(DatabaseConnectionInfo? value)
    {
        if (_suppressSelectionHandling)
            return;

        if (value is null)
        {
            if (!IsNew)
                CloseForm();
            return;
        }

        StartAsBusy(() => OpenExistingAsync(value));
    }

    private async Task OpenExistingAsync(DatabaseConnectionInfo info)
    {
        SetStatus(string.Empty);

        _editingId = info.Id;
        _editingProvider = info.Provider;
        EditName = info.Name;
        EditConnectionString = await _store.GetConnectionStringAsync(info.Id, LifetimeToken) ?? string.Empty;

        IsNew = false;
        IsEditing = true;

        if (string.IsNullOrEmpty(EditConnectionString))
            SetStatus("Chaine introuvable dans le coffre : saisissez-la de nouveau.", isError: true);
    }

    [RelayCommand]
    private void New()
    {
        SelectedConnection = null;

        _editingId = Guid.NewGuid();
        _editingProvider = DatabaseProvider.SqlServer;
        EditName = string.Empty;
        EditConnectionString = string.Empty;

        IsNew = true;
        IsEditing = true;
        SetStatus(string.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseForm();

    [RelayCommand]
    private Task SaveAsync() => RunAsBusyAsync(SaveBusyAsync, skipIfBusy: true);

    private async Task SaveBusyAsync()
    {
        var name = EditName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            SetStatus("Donnez un nom a la connexion.", isError: true);
            return;
        }

        var info = new DatabaseConnectionInfo
        {
            Id = _editingId,
            Name = name,
            Provider = _editingProvider,
        };

        var connectionString = EditConnectionString?.Trim() ?? string.Empty;
        var error = _testerResolver.Resolve(_editingProvider).Validate(connectionString, info);
        if (error is not null)
        {
            SetStatus(error, isError: true);
            return;
        }

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
        info ??= SelectedConnection;
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
        var connectionString = EditConnectionString?.Trim() ?? string.Empty;
        var tester = _testerResolver.Resolve(_editingProvider);

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
        IsEditing = false;
        IsNew = false;
        _editingId = Guid.Empty;
        EditName = string.Empty;
        EditConnectionString = string.Empty;
        SetStatus(string.Empty);

        if (SelectedConnection is not null)
        {
            _suppressSelectionHandling = true;
            SelectedConnection = null;
            _suppressSelectionHandling = false;
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }
}
