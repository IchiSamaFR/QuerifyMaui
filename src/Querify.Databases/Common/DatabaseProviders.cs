using Querify.Databases.Constants;
using Querify.Databases.Enums;

namespace Querify.Databases.Common;

/// <summary>Libelle et exemple de chaine de connexion de chaque moteur.</summary>
public static class DatabaseProviders
{
    /// <summary>Moteurs proposes a la creation d'une connexion, dans l'ordre d'affichage.</summary>
    public static IReadOnlyDictionary<DatabaseProvider, string> Labels { get; } = new Dictionary<DatabaseProvider, string>
    {
        [DatabaseProvider.SqlServer] = "SQL Server",
        [DatabaseProvider.MySql] = "MySQL / MariaDB",
        [DatabaseProvider.PostgreSql] = "PostgreSQL",
        [DatabaseProvider.Oracle] = "Oracle",
        [DatabaseProvider.Sqlite] = "SQLite",
    };

    public static string GetLabel(DatabaseProvider provider)
        => Labels.TryGetValue(provider, out var label) ? label : provider.ToString();

    public static string GetConnectionStringExample(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.MySql => ConnectionStringExamples.MySql,
        DatabaseProvider.PostgreSql => ConnectionStringExamples.PostgreSql,
        DatabaseProvider.Oracle => ConnectionStringExamples.Oracle,
        DatabaseProvider.Sqlite => ConnectionStringExamples.Sqlite,
        _ => ConnectionStringExamples.SqlServer,
    };
}
