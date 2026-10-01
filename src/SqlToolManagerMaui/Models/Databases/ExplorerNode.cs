using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MauiIcons.Fluent;

namespace SqlToolManagerMaui.Models.Databases;

/// <summary>
/// Noeud de l'explorateur d'objets (facon SSMS), affiche par le TreeView de SqlToolManagerMaui.UI
/// via <see cref="Children"/>, <see cref="IsExpanded"/> et <see cref="CanExpand"/>.
/// Aucun noeud ne porte la chaine de connexion : elle est relue dans le coffre a chaque chargement.
/// </summary>
public sealed partial class ExplorerNode : ObservableObject
{
    public ExplorerNode(ExplorerNodeKind kind, string title, DatabaseConnectionInfo connection, ExplorerNode? parent = null)
    {
        Kind = kind;
        Title = title;
        Connection = connection;
        Parent = parent;
        Database = parent?.Database;
    }

    public ExplorerNodeKind Kind { get; }

    public string Title { get; }

    public DatabaseConnectionInfo Connection { get; }

    public ExplorerNode? Parent { get; }

    /// <summary>Base a laquelle appartient le noeud (herite du parent).</summary>
    public string? Database { get; init; }

    /// <summary>Schema de l'objet (tables, vues, procedures).</summary>
    public string? Schema { get; init; }

    /// <summary>Precision affichee en gris a droite du titre (serveur, nombre d'elements...).</summary>
    [ObservableProperty]
    public partial string? Detail { get; set; }

    public ObservableCollection<ExplorerNode> Children { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Icon))]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Vrai une fois les enfants charges (un rafraichissement le remet a faux).</summary>
    public bool IsLoaded { get; set; }

    public bool CanExpand => Kind is not (ExplorerNodeKind.Table or ExplorerNodeKind.View
        or ExplorerNodeKind.StoredProcedure or ExplorerNodeKind.Message);

    public bool IsConnection => Kind == ExplorerNodeKind.Connection;

    /// <summary>Famille d'objets chargee par un dossier, ou null pour les autres noeuds.</summary>
    public DatabaseObjectKind? FolderObjectKind => Kind switch
    {
        ExplorerNodeKind.TablesFolder => DatabaseObjectKind.Table,
        ExplorerNodeKind.ViewsFolder => DatabaseObjectKind.View,
        ExplorerNodeKind.StoredProceduresFolder => DatabaseObjectKind.StoredProcedure,
        _ => null,
    };

    public FluentIcons Icon => Kind switch
    {
        ExplorerNodeKind.Connection => FluentIcons.Server20,
        ExplorerNodeKind.Database => FluentIcons.Database20,
        ExplorerNodeKind.Table => FluentIcons.Table20,
        ExplorerNodeKind.View => FluentIcons.Eye20,
        ExplorerNodeKind.StoredProcedure => FluentIcons.Code20,
        ExplorerNodeKind.Message => FluentIcons.Info16,
        _ => IsExpanded ? FluentIcons.FolderOpen20 : FluentIcons.Folder20,
    };

}
