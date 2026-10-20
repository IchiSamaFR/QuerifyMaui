using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Services.Databases;

namespace SqlToolManagerMaui.ViewModels.Pages.Shared;

/// <summary>Connexion choisie dans l'en-tete des onglets : une seule pour toute la page.</summary>
[AutoRegister]
public partial class DatabaseConnectionVM : ObservableObject
{
    private readonly IConnectionStore _connectionStore;
    private readonly StatusBarVM _status;

    public DatabaseConnectionVM(IConnectionStore connectionStore, StatusBarVM status)
    {
        _connectionStore = connectionStore;
        _status = status;

        // Ajout, modification ou suppression depuis l'explorateur : le Picker suit le coffre.
        _connectionStore.Subscribe(ApplyConnections);
    }

    /// <summary>Leve apres chaque changement de connexion (les onglets ouverts suivent).</summary>
    public event EventHandler? SelectedConnectionChanged;

    /// <summary>Connexions proposees : les instances partagees du store.</summary>
    [ObservableProperty]
    public partial List<DatabaseConnectionInfo> Connections { get; set; } = [];

    /// <summary>Element selectionne dans le Picker ; null = aucune connexion (Title du Picker).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveConnection))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectCommand))]
    public partial DatabaseConnectionInfo? SelectedConnection { get; set; }

    /// <summary>Connexion reellement retenue, ou null si l'utilisateur est deconnecte.</summary>
    public DatabaseConnectionInfo? ActiveConnection => SelectedConnection;

    public bool IsConnected => ActiveConnection is not null;

    public Task RefreshAsync(CancellationToken token) => _connectionStore.RefreshAsync(token);

    /// <summary>Selectionne la connexion du meme identifiant, si elle existe encore.</summary>
    public void Select(Guid connectionId)
        => SelectedConnection = Connections.FirstOrDefault(c => c.Id == connectionId) ?? SelectedConnection;

    /// <summary>Selectionne la connexion de ce nom (fichiers .schx), si elle existe.</summary>
    public void SelectByName(string name)
    {
        if (Connections.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) is { } connection)
            SelectedConnection = connection;
    }

    private void ApplyConnections(IReadOnlyList<DatabaseConnectionInfo> connections)
    {
        var selectedConnectionId = SelectedConnection?.Id;

        Connections = connections.ToList();

        if (selectedConnectionId is { } id)
            Select(id);
    }

    /// <summary>Reinitialise la selection du Picker (aucune connexion).</summary>
    [RelayCommand(CanExecute = nameof(IsConnected))]
    private void Disconnect()
    {
        SelectedConnection = null;
    }

    partial void OnSelectedConnectionChanged(DatabaseConnectionInfo? value)
    {
        _status.Set(value is null
            ? "Deconnecte."
            : $"Connexion active : {value.Name}.");

        if (value is not null)
            _connectionStore.OpenConnectionAsync(value);

        SelectedConnectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
