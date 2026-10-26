using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Databases.Connections;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.UI.Extensions;

namespace SqlToolManagerMaui.ViewModels.Pages.Shared;

/// <summary>
/// Connexion choisie dans l'en-tete des onglets. Le Picker est partage, mais chaque onglet garde sa
/// connexion (<see cref="AbstractSession.ConnectionId"/>) : il la retrouve quand il redevient visible.
/// </summary>
[AutoRegister]
public partial class DatabaseConnectionVM : ObservableObject
{
    private readonly IConnectionStore _connectionStore;
    private readonly StatusBarVM _status;

    /// <summary>Onglet affiche, dont la connexion suit le Picker.</summary>
    private AbstractSession? _session;

    /// <summary>Vrai pendant un changement d'onglet : la connexion de l'onglet affiche, elle, n'a pas change.</summary>
    private bool _isAttaching;

    public DatabaseConnectionVM(IConnectionStore connectionStore, StatusBarVM status)
    {
        _connectionStore = connectionStore;
        _status = status;

        // Ajout, modification ou suppression depuis l'explorateur : le Picker suit le coffre.
        _connectionStore.Subscribe(ApplyConnections).FireAndForget("Liste des connexions non chargée.");
    }

    /// <summary>Leve quand l'utilisateur change la connexion de l'onglet affiche (pas lors d'un changement d'onglet).</summary>
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

    /// <summary>Affiche dans le Picker la connexion de l'onglet ; sans onglet, la selection reste.</summary>
    public void AttachSession(AbstractSession? session)
    {
        _session = session;
        if (session is null)
            return;

        _isAttaching = true;
        try
        {
            SelectedConnection = session.ConnectionId is { } id ? Connections.FirstOrDefault(c => c.Id == id) : null;
        }
        finally
        {
            _isAttaching = false;
        }
    }

    private void ApplyConnections(IReadOnlyList<DatabaseConnectionInfo> connections)
    {
        var selectedConnectionId = SelectedConnection?.Id;

        Connections = connections.ToList();

        // Liste chargee apres la restauration des onglets, ou connexion supprimee : l'onglet retrouve la sienne.
        if (_session is not null)
            AttachSession(_session);
        else if (selectedConnectionId is { } id)
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
        // Ouverture anticipee : l'execution la refait de toute facon (await) si elle a echoue.
        if (value is not null)
            _connectionStore.OpenConnectionAsync(value).FireAndForget($"Connexion {value.Name} non ouverte.");

        if (_isAttaching)
            return;

        if (_session is not null)
            _session.ConnectionId = value?.Id;

        _status.Set(value is null
            ? "Deconnecte."
            : $"Connexion active : {value.Name}.");

        SelectedConnectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
