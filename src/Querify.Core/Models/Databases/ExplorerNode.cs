using CommunityToolkit.Mvvm.ComponentModel;
using Querify.Core.Enums.Databases;
using Querify.Databases.Models.Connections;
using System.Collections.ObjectModel;
using Quill.Data.Sql;

namespace Querify.Core.Models.Databases;

/// <summary>
/// Noeud de l'explorateur d'objets (facon SSMS), affiche par le TreeView de Querify.UI
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
        Schema = parent?.Schema;
        ObjectName = parent?.ObjectName;
    }

    public ExplorerNodeKind Kind { get; }

    public string Title { get; }

    public DatabaseConnectionInfo Connection { get; }

    public ExplorerNode? Parent { get; }

    /// <summary>Base a laquelle appartient le noeud (herite du parent).</summary>
    public string? Database { get; init; }

    /// <summary>Schema de l'objet (tables, vues, procedures), herite par les dossiers et colonnes.</summary>
    public string? Schema { get; init; }

    /// <summary>Nom de l'objet sans schema (tables, vues, procedures), herite par les dossiers et colonnes.</summary>
    public string? ObjectName { get; init; }

    /// <summary>Colonne de cle primaire (icone cle).</summary>
    public bool IsPrimaryKey { get; init; }

    /// <summary>Colonne de cle etrangere (icone lien).</summary>
    public bool IsForeignKey { get; init; }

    /// <summary>Commentaire de l'objet ou de la colonne (affiche en infobulle), null si absent.</summary>
    public string? Comment { get; init; }

    /// <summary>Precision affichee en gris a droite du titre (serveur, nombre d'elements...).</summary>
    [ObservableProperty]
    public partial string? Detail { get; set; }

    public ObservableCollection<ExplorerNode> Children { get; } = [];

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>Vrai une fois les enfants charges (un rafraichissement le remet a faux).</summary>
    public bool IsLoaded { get; set; }

    public bool CanExpand => Kind is not (ExplorerNodeKind.StoredProcedure
        or ExplorerNodeKind.Column or ExplorerNodeKind.Message);

    public bool IsConnection => Kind == ExplorerNodeKind.Connection;

    /// <summary>Famille d'objets chargee par un dossier, ou null pour les autres noeuds.</summary>
    public SqlSchemaObjectKind? FolderObjectKind => Kind switch
    {
        ExplorerNodeKind.TablesFolder => SqlSchemaObjectKind.Table,
        ExplorerNodeKind.ViewsFolder => SqlSchemaObjectKind.View,
        ExplorerNodeKind.StoredProceduresFolder => SqlSchemaObjectKind.Procedure,
        _ => null,
    };
}
