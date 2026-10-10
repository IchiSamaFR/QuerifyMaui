using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Services.Databases;

namespace SqlToolManagerMaui.ViewModels.Pages.Queries;

/// <summary>Connexion choisie dans le Picker de la page Requete, partagee par l'execution et les dashboards.</summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class QueryConnectionVM : ObservableObject
{
    private readonly IConnectionStore _connectionStore;

    // Fourni par Attach : QueryVM recoit ce VM par injection avant de pouvoir se passer en hote.
    private IQueryWorkspace _workspace = null!;

    public QueryConnectionVM(IConnectionStore connectionStore)
    {
        _connectionStore = connectionStore;
    }

    /// <summary>Levé apres chaque changement de connexion (execution et cartes du dashboard suivent).</summary>
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

    /// <summary>Branche le VM sur la page hote ; appele une seule fois, par QueryVM.</summary>
    public void Attach(IQueryWorkspace workspace)
    {
        _workspace = workspace;

        // Ajout, modification ou suppression depuis l'explorateur : le Picker suit le coffre.
        _connectionStore.Subscribe(ApplyConnections);
    }

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
        _workspace.Status.Set(value is null
            ? "Deconnecte."
            : $"Connexion active : {value.Name}.");

        if (value is not null)
            _connectionStore.OpenConnectionAsync(value);

        SelectedConnectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
