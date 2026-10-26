using Querify.Databases.Models.Connections;
using Querify.Languages.Linq.Modeling;

namespace Querify.Languages.Queries;

/// <summary>
/// Lecture des premieres lignes d'une table, demandee depuis l'explorateur : le destinataire
/// la traduit dans le langage de l'onglet qui l'accueille.
/// </summary>
public sealed record InsertQueryMessage(DatabaseConnectionInfo Connection, string Database, string Schema, string Table, int RowCount)
{
    /// <summary>Nom complet avec la base : la requete reste valable quelle que soit la base par defaut.</summary>
    public string ToSql() => $"SELECT TOP ({RowCount}) *\nFROM {Quote(Database)}.{Quote(Schema)}.{Quote(Table)};";

    /// <summary>
    /// Contexte nomme d'apres la base et entite d'apres la table, comme le DbContext genere.
    /// Une table homonyme dans un autre schema y est prefixee du schema : le nom est alors a ajuster.
    /// </summary>
    public string ToLinq() => $"{EfModelGenerator.ToIdentifier(Database)}.{EfModelGenerator.ToIdentifier(Table)}.Take({RowCount})";

    private static string Quote(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
}
