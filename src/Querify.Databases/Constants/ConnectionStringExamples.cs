namespace Querify.Databases.Constants;

/// <summary>Exemple de chaine de connexion par moteur, affiche dans le formulaire vide.</summary>
public static class ConnectionStringExamples
{
    public const string SqlServer = "Server=.;Database=MaBase;Trusted_Connection=True;TrustServerCertificate=True";

    public const string MySql = "Server=localhost;Port=3306;Database=ma_base;User ID=root;Password=...";

    public const string PostgreSql = "Host=localhost;Port=5432;Database=ma_base;Username=postgres;Password=...";

    public const string Oracle = "Data Source=localhost:1521/FREEPDB1;User Id=mon_schema;Password=...";

    public const string Sqlite = @"Data Source=C:\Donnees\ma_base.db";
}
