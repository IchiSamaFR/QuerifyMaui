namespace Querify.Databases.Enums;

/// <summary>
/// Moteur de base de donnees cible d'une connexion (valeur ecrite dans connections.json : ne jamais renumeroter).
/// Un nouveau moteur s'ajoute ici, puis dans son testeur de connexion et dans AddDatabases.
/// </summary>
public enum DatabaseProvider
{
    SqlServer = 0,
    MySql = 1,
    PostgreSql = 2,
    Oracle = 3,
    Sqlite = 4,
}
