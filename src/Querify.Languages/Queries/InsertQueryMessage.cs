using Querify.Databases.Models.Connections;

namespace Querify.Languages.Queries;

/// <summary>
/// Lecture des premieres lignes d'une table, demandee depuis l'explorateur : le destinataire la fait ecrire par le
/// langage de l'onglet qui l'accueille (<c>IEditorLanguage.CreateSelectFirstRows</c>), dans le dialecte du moteur.
/// </summary>
public sealed record InsertQueryMessage(DatabaseConnectionInfo Connection, string Database, string Schema, string Table, int RowCount);
