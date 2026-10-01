using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.Models.Databases;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlToolManagerMaui.ViewModels.Pages.Connections;

/// <summary>
/// Explorateur d'objets facon SSMS : Serveur > Bases (+ dossier systeme) > Tables / Vues /
/// Procedures stockees > objets, affiche par le TreeView de SqlToolManagerMaui.UI.
/// Le TreeView gere l'ouverture (IsExpanded) et appelle <see cref="ExpandNodeCommand"/> :
/// chaque niveau est charge a la premiere ouverture du noeud. La creation et la modification
/// des connexions passent par la popup "Se connecter" (<see cref="IConnectionDialogService"/>).
/// </summary>
[AutoRegister]
public partial class ConnectionExplorerVM : AbstractVM
{
    private readonly IConnectionStore _store;
    private readonly IDatabaseSchemaReaderResolver _schemaReaders;
    private readonly IConnectionDialogService _dialogs;

    /// <summary>Noeuds racines : une entree par connexion enregistree.</summary>
    public ObservableCollection<ExplorerNode> Roots { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnectionSelected))]
    [NotifyCanExecuteChangedFor(nameof(EditConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteConnectionCommand))]
    public partial ExplorerNode? SelectedNode { get; set; }

    public bool IsConnectionSelected => SelectedNode?.IsConnection == true;

    public bool HasConnections => Roots.Count > 0;

    public ConnectionExplorerVM(IConnectionStore store, IDatabaseSchemaReaderResolver schemaReaders, IConnectionDialogService dialogs)
    {
        _store = store;
        _schemaReaders = schemaReaders;
        _dialogs = dialogs;
    }

    protected override Task LoadDatasBusyAsync() => ReloadConnectionsAsync();

    // ---------- Connexions ----------

    /// <summary>Reconstruit les racines en conservant l'ouverture des connexions deja deployees.</summary>
    private async Task ReloadConnectionsAsync()
    {
        var connections = await _store.GetAllAsync(LifetimeToken);
        var expanded = Roots.Where(r => r.IsExpanded).Select(r => r.Connection.Id).ToHashSet();
        var selectedId = SelectedNode?.Connection.Id;

        Roots.Clear();
        foreach (var connection in connections)
        {
            Roots.Add(new ExplorerNode(ExplorerNodeKind.Connection, connection.Name, connection)
            {
                Detail = connection.Server,
            });
        }

        OnPropertyChanged(nameof(HasConnections));

        SelectedNode = Roots.FirstOrDefault(r => r.Connection.Id == selectedId);

        // En parallele et sans bloquer : un serveur injoignable ne doit pas retenir les autres.
        // IsExpanded passe a vrai : le TreeView declenche ExpandNodeCommand pour chacune.
        foreach (var root in Roots.Where(r => expanded.Contains(r.Connection.Id)))
            root.IsExpanded = true;
    }

    [RelayCommand]
    private Task AddConnectionAsync() => EditAsync(null);

    [RelayCommand(CanExecute = nameof(IsConnectionSelected))]
    private Task EditConnectionAsync() => EditAsync(SelectedNode?.Connection);

    private async Task EditAsync(DatabaseConnectionInfo? connection)
    {
        var savedId = await _dialogs.EditAsync(connection, LifetimeToken);
        if (savedId is null)
            return;

        await ReloadConnectionsAsync();
        SelectedNode = Roots.FirstOrDefault(r => r.Connection.Id == savedId);
    }

    [RelayCommand(CanExecute = nameof(IsConnectionSelected))]
    private Task DeleteConnectionAsync() => RunAsBusyAsync(DeleteBusyAsync, skipIfBusy: true);

    private async Task DeleteBusyAsync()
    {
        var info = SelectedNode?.Connection;
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
        WeakReferenceMessenger.Default.Send(new ConnectionsChangedMessage());

        SelectedNode = null;
        await ReloadConnectionsAsync();
    }

    // ---------- Arbre ----------

    /// <summary>Appele par le TreeView a chaque ouverture : ne charge que la premiere fois.</summary>
    [RelayCommand]
    private Task ExpandNodeAsync(ExplorerNode? node)
    {
        if (node is null || node.IsLoaded || node.IsLoading)
            return Task.CompletedTask;

        return RunAsBusyAsync(() => LoadAsync(node), blocking: false);
    }

    /// <summary>Recharge le noeud selectionne (ou toutes les connexions si rien n'est selectionne).</summary>
    [RelayCommand]
    private Task RefreshAsync()
    {
        var node = SelectedNode;

        // Feuilles et dossier systeme (rempli par le serveur) : on recharge le parent.
        while (node is not null && (!node.CanExpand || node.Kind == ExplorerNodeKind.SystemDatabasesFolder))
            node = node.Parent;

        if (node is null)
            return RunAsBusyAsync(ReloadConnectionsAsync, blocking: false);

        node.IsLoaded = false;
        node.Children.Clear();

        if (node.IsExpanded)
            return RunAsBusyAsync(() => LoadAsync(node), blocking: false);

        // L'ouverture declenche ExpandNodeCommand, qui recharge le noeud.
        node.IsExpanded = true;
        return Task.CompletedTask;
    }

    private async Task LoadAsync(ExplorerNode node)
    {
        node.IsLoading = true;
        try
        {
            var children = await LoadChildrenAsync(node);
            node.Children.Clear();
            foreach (var child in children)
                node.Children.Add(child);
            node.IsLoaded = true;
        }
        finally
        {
            node.IsLoading = false;
        }
    }

    // ---------- Chargement des enfants ----------

    private async Task<IReadOnlyList<ExplorerNode>> LoadChildrenAsync(ExplorerNode node)
    {
        try
        {
            return node.Kind switch
            {
                ExplorerNodeKind.Connection => await LoadDatabasesAsync(node),
                ExplorerNodeKind.SystemDatabasesFolder => node.Children.ToList(),
                ExplorerNodeKind.Database => CreateFolders(node),
                ExplorerNodeKind.Table or ExplorerNodeKind.View => [new ExplorerNode(ExplorerNodeKind.ColumnsFolder, "Colonnes", node.Connection, node)],
                ExplorerNodeKind.ColumnsFolder => await LoadColumnsAsync(node),
                _ when node.FolderObjectKind is { } kind => await LoadObjectsAsync(node, kind),
                _ => [],
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Serveur injoignable, base hors ligne, droits insuffisants : l'erreur devient un noeud.
            return [Message(node, $"Erreur : {ex.Message}")];
        }
    }

    private async Task<IReadOnlyList<ExplorerNode>> LoadDatabasesAsync(ExplorerNode server)
    {
        var connectionString = await GetConnectionStringAsync(server.Connection);
        if (connectionString is null)
            return [Message(server, "Chaine introuvable dans le coffre : modifiez la connexion.")];

        var databases = await _schemaReaders.Resolve(server.Connection.Provider)
            .GetDatabasesAsync(connectionString, LifetimeToken);

        var result = new List<ExplorerNode>();

        // Comme SSMS : les bases systeme sont regroupees dans un dossier, deja charge.
        var systemDatabases = databases.Where(d => d.IsSystem).ToList();
        if (systemDatabases.Count > 0)
        {
            var folder = new ExplorerNode(ExplorerNodeKind.SystemDatabasesFolder, "Bases de donnees systeme", server.Connection, server);
            foreach (var database in systemDatabases)
                folder.Children.Add(DatabaseNode(database.Name, folder));
            folder.IsLoaded = true;
            result.Add(folder);
        }

        result.AddRange(databases.Where(d => !d.IsSystem).Select(d => DatabaseNode(d.Name, server)));

        server.Detail = $"{server.Connection.Server} ({databases.Count(d => !d.IsSystem)})";
        return result;
    }

    private static ExplorerNode DatabaseNode(string name, ExplorerNode parent)
        => new(ExplorerNodeKind.Database, name, parent.Connection, parent) { Database = name };

    private static IReadOnlyList<ExplorerNode> CreateFolders(ExplorerNode database) =>
    [
        new(ExplorerNodeKind.TablesFolder, "Tables", database.Connection, database),
        new(ExplorerNodeKind.ViewsFolder, "Vues", database.Connection, database),
        new(ExplorerNodeKind.StoredProceduresFolder, "Procedures stockees", database.Connection, database),
    ];

    private async Task<IReadOnlyList<ExplorerNode>> LoadObjectsAsync(ExplorerNode folder, DatabaseObjectKind kind)
    {
        var connectionString = await GetConnectionStringAsync(folder.Connection);
        if (connectionString is null || folder.Database is null)
            return [Message(folder, "Chaine introuvable dans le coffre : modifiez la connexion.")];

        var objects = await _schemaReaders.Resolve(folder.Connection.Provider)
            .GetObjectsAsync(connectionString, folder.Database, kind, LifetimeToken);

        folder.Detail = objects.Count.ToString();
        if (objects.Count == 0)
            return [Message(folder, "(aucun element)")];

        var nodeKind = kind switch
{
            DatabaseObjectKind.Table => ExplorerNodeKind.Table,
            DatabaseObjectKind.View => ExplorerNodeKind.View,
            _ => ExplorerNodeKind.StoredProcedure,
        };

        return objects
            .Select(o => new ExplorerNode(nodeKind, o.QualifiedName, folder.Connection, folder) { Schema = o.Schema, ObjectName = o.Name })
            .ToList();
    }

    private async Task<IReadOnlyList<ExplorerNode>> LoadColumnsAsync(ExplorerNode folder)
    {
        var connectionString = await GetConnectionStringAsync(folder.Connection);
        if (connectionString is null || folder.Database is null || folder.Schema is null || folder.ObjectName is null)
            return [Message(folder, "Chaine introuvable dans le coffre : modifiez la connexion.")];

        var columns = await _schemaReaders.Resolve(folder.Connection.Provider)
            .GetColumnsAsync(connectionString, folder.Database, folder.Schema, folder.ObjectName, LifetimeToken);

        folder.Detail = columns.Count.ToString();
        if (columns.Count == 0)
            return [Message(folder, "(aucune colonne)")];

        return columns
            .Select(c => new ExplorerNode(ExplorerNodeKind.Column, c.Name, folder.Connection, folder)
            {
                Detail = $"({c.Description})",
                IsPrimaryKey = c.IsPrimaryKey,
                IsForeignKey = c.IsForeignKey,
            })
            .ToList();
    }

    private static ExplorerNode Message(ExplorerNode parent, string text)
        => new(ExplorerNodeKind.Message, text, parent.Connection, parent);

    private Task<string?> GetConnectionStringAsync(DatabaseConnectionInfo connection)
        => _store.GetConnectionStringAsync(connection.Id, LifetimeToken);
}
