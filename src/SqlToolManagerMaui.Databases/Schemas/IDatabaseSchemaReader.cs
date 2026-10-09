using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Databases.Schemas;

namespace SqlToolManagerMaui.Databases.Schemas;

/// <summary>
/// Lecture du catalogue d'un serveur pour l'explorateur d'objets.
/// Une implementation par moteur, resolue par <see cref="Provider"/>.
/// </summary>
public interface IDatabaseSchemaReader
{
    DatabaseProvider Provider { get; }

    /// <summary>Bases en ligne du serveur, triees par nom.</summary>
    Task<IReadOnlyList<DatabaseCatalogEntry>> GetDatabasesAsync(string connectionString, CancellationToken cancellationToken = default);

    /// <summary>Objets utilisateur d'une famille donnee, tries par schema puis nom.</summary>
    Task<IReadOnlyList<DatabaseObjectEntry>> GetObjectsAsync(string connectionString, string database, DatabaseObjectKind kind, CancellationToken cancellationToken = default);

    /// <summary>Colonnes d'une table ou d'une vue, dans l'ordre de declaration.</summary>
    Task<IReadOnlyList<DatabaseColumnEntry>> GetColumnsAsync(string connectionString, string database, string schema, string objectName, CancellationToken cancellationToken = default);

    /// <summary>Tables, vues, procedures et colonnes d'une base en un aller-retour ; null = base de la connexion.</summary>
    Task<DatabaseSchemaSnapshot> GetSchemaSnapshotAsync(string connectionString, string? database, CancellationToken cancellationToken = default);
}
