using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SqlToolManagerMaui.Core.Enums.Databases;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Databases;
using SqlToolManagerMaui.Databases.Enums;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Schemas;
using SqlToolManagerMaui.Helpers.Extensions;
using SqlToolManagerMaui.Languages.Queries;
using SqlToolManagerMaui.Services.Databases;
using SqlToolManagerMaui.Services.Dialogs;
using SqlToolManagerMaui.UI.Extensions;
using SqlToolManagerMaui.UI.ViewModels;
using System.Collections.ObjectModel;

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
    private readonly IDialogService _questions;

    /// <summary>Rechargement lance par <see cref="ConnectionsChangedMessage"/>, attendu par l'edition pour reselectionner.</summary>
    private Task _connectionsReload = Task.CompletedTask;

    /// <summary>Noeuds racines : une entree par connexion enregistree.</summary>
    public ObservableCollection<ExplorerNode> Roots { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnectionSelected))]
    [NotifyCanExecuteChangedFor(nameof(EditConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteConnectionCommand))]
    public partial ExplorerNode? SelectedNode { get; set; }

    public bool IsConnectionSelected => SelectedNode?.IsConnection == true;

    public ConnectionExplorerVM(IConnectionStore store, IDatabaseSchemaReaderResolver schemaReaders, IConnectionDialogService dialogs, IDialogService questions)
    {
        _store = store;
        _store.Subscribe(ApplyConnections).FireAndForget("Liste des connexions non chargée.");
        _schemaReaders = schemaReaders;
        _dialogs = dialogs;
        _questions = questions;
    }

    protected override Task LoadDatasBusyAsync() => ReloadConnectionsAsync();

    // ---------- Connexions ----------

    private void ApplyConnections(IReadOnlyList<DatabaseConnectionInfo> connections)
    {
        // On conserve la selection si possible.
        var selectedId = SelectedNode?.Connection.Id;

        // On supprime les racines dont la connexion a ete supprimee.
        var connectionIds = connections.Select(c => c.Id).ToHashSet();

        var removedNodes = Roots
            .Where(r => !connectionIds.Contains(r.Connection.Id))
            .ToList();
        Roots.RemoveRange(removedNodes);

        // On ajoute les nouvelles connexions, sans toucher aux racines deja presentes (et eventuellement ouvertes).
        var missingConnections = connections
            .Where(c => !Roots.Any(r => r.Connection.Id == c.Id))
            .ToList();
        foreach (var c in missingConnections.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            var node = new ExplorerNode(ExplorerNodeKind.Connection, c.Name, c) { Detail = c.Server };
            var insertIndex = Roots.ToList().FindIndex(r => string.Compare(r.Connection.Name, c.Name, StringComparison.OrdinalIgnoreCase) > 0);
            if (insertIndex < 0)
            {
                Roots.Add(node);
            }
            else
            {
                Roots.Insert(insertIndex, node);
            }
        }

        // On reordonne les racines existantes si un nom a change (sans recreer les noeuds, pour preserver IsExpanded).
        var orderedIds = Roots
            .OrderBy(r => r.Connection.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => r.Connection.Id)
            .ToList();
        for (var targetIndex = 0; targetIndex < orderedIds.Count; targetIndex++)
        {
            var currentIndex = Roots.ToList().FindIndex(r => r.Connection.Id == orderedIds[targetIndex]);
            if (currentIndex != targetIndex)
            {
                Roots.Move(currentIndex, targetIndex);
            }
        }

        // On reselectionne la connexion precedemment selectionnee si elle existe toujours.
        SelectedNode = Roots.FirstOrDefault(r => r.Connection.Id == selectedId);
    }

    /// <summary>Reconstruit les racines en conservant l'ouverture des connexions deja deployees.</summary>
    private async Task ReloadConnectionsAsync()
    {
        // RefreshAsync notifie les abonnes (ApplyConnections), qui ajoute/supprime
        // les racines sans toucher aux noeuds existants : IsExpanded est donc preserve.
        await _store.RefreshAsync(LifetimeToken);
    }

    [RelayCommand]
    private Task AddConnectionAsync() => EditAsync(null);

    // Parametre fourni par le menu contextuel ; null depuis la barre d'outils (noeud selectionne).
    private bool CanEditConnection(ExplorerNode? node) => (node ?? SelectedNode)?.IsConnection == true;

    [RelayCommand(CanExecute = nameof(CanEditConnection))]
    private Task EditConnectionAsync(ExplorerNode? node) => EditAsync((node ?? SelectedNode)?.Connection);

    private async Task EditAsync(DatabaseConnectionInfo? connection)
    {
        var savedId = await _dialogs.EditAsync(connection, LifetimeToken);
        if (savedId is null)
            return;

        // La popup a publie ConnectionsChangedMessage : on attend le rechargement qu'il a lance.
        await _connectionsReload;
        SelectedNode = Roots.FirstOrDefault(r => r.Connection.Id == savedId);
    }

    /// <summary>Ferme la connexion : vide la chaine en memoire et replie l'arbre du serveur.</summary>
    [RelayCommand(CanExecute = nameof(CanEditConnection))]
    private void CloseConnection(ExplorerNode? node)
    {
        var root = node ?? SelectedNode;
        if (root is not { IsConnection: true })
            return;

        _store.CloseConnection(root.Connection);

        root.IsExpanded = false;
        root.Children.Clear();
        root.IsLoaded = false;
        root.Detail = root.Connection.Server;
    }

    [RelayCommand(CanExecute = nameof(CanEditConnection))]
    private Task DeleteConnectionAsync(ExplorerNode? node)
        => RunAsBusyAsync(() => DeleteBusyAsync((node ?? SelectedNode)?.Connection), skipIfBusy: true);

    private async Task DeleteBusyAsync(DatabaseConnectionInfo? info)
    {
        if (info is null)
            return;

        var confirmed = await _questions.ConfirmAsync(
            "Supprimer la connexion",
            $"Supprimer définitivement \"{info.Name}\" ? La chaîne de connexion sera effacée du coffre.",
            "Supprimer");

        if (!confirmed)
            return;

        await _store.DeleteAsync(info.Id, LifetimeToken);

        SelectedNode = null;
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

    /// <summary>Recharge le noeud donne ou selectionne (ou toutes les connexions a defaut).</summary>
    [RelayCommand]
    private Task RefreshAsync(ExplorerNode? node)
    {
        node ??= SelectedNode;

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

    /// <summary>Referme tout l'arbre (les enfants deja charges sont conserves).</summary>
    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var root in Roots)
            Collapse(root);

        static void Collapse(ExplorerNode node)
        {
            foreach (var child in node.Children)
                Collapse(child);

            node.IsExpanded = false;
        }
    }

    // ---------- Requetes ----------

    [RelayCommand]
    private void SelectTop1(ExplorerNode? table) => SendSelectTop(table, 1);

    [RelayCommand]
    private void SelectTop100(ExplorerNode? table) => SendSelectTop(table, 100);

    private static void SendSelectTop(ExplorerNode? table, int count)
    {
        if (table is not { Kind: ExplorerNodeKind.Table, Database: { } database, Schema: { } schema, ObjectName: { } name })
            return;

        // La requete est ecrite par MainVM, dans le langage de l'onglet qui l'accueille.
        WeakReferenceMessenger.Default.Send(new InsertQueryMessage(table.Connection, database, schema, name, count));
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
        // Ouvre la connexion a la demande : l'expansion du serveur vaut connexion.
        if (!await _store.OpenConnectionAsync(server.Connection, LifetimeToken))
            return [Message(server, "Chaine introuvable dans le coffre : modifiez la connexion.")];

        var connectionString = server.Connection.ConnectionString!;
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
        var connectionString = folder.Connection.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString) || folder.Database is null)
            return [Message(folder, "Connexion fermee : rouvrez le serveur.")];

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
            .Select(o => new ExplorerNode(nodeKind, o.QualifiedName, folder.Connection, folder) { Schema = o.Schema, ObjectName = o.Name, Comment = o.Comment })
            .ToList();
    }

    private async Task<IReadOnlyList<ExplorerNode>> LoadColumnsAsync(ExplorerNode folder)
    {
        var connectionString = folder.Connection.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString) || folder.Database is null || folder.Schema is null || folder.ObjectName is null)
            return [Message(folder, "Connexion fermee : rouvrez le serveur.")];

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
                Comment = c.Comment,
            })
            .ToList();
    }

    private static ExplorerNode Message(ExplorerNode parent, string text)
        => new(ExplorerNodeKind.Message, text, parent.Connection, parent);
}
