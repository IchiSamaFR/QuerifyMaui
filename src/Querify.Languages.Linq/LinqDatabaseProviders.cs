using Querify.Databases.Enums;
using Querify.Databases.Models.Connections;
using Quill.Code.CSharp.Linq;
using Quill.Code.CSharp.Linq.SqlServer;

namespace Querify.Languages.Linq;

/// <summary>Moteurs LINQ de Quill par moteur de connexion.</summary>
internal static class LinqDatabaseProviders
{
    private const string ApplicationName = "Querify";

    // Une instance par moteur : l'editeur et l'execution partagent ainsi le meme DbContext compile.
    private static readonly Dictionary<DatabaseProvider, ILinqDatabaseProvider> Providers = new()
    {
        [DatabaseProvider.SqlServer] = new SqlServerLinqProvider(ApplicationName),
    };

    /// <summary>Moteur LINQ de la connexion, null s'il n'en a pas.</summary>
    public static ILinqDatabaseProvider? Find(DatabaseConnectionInfo connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return Providers.GetValueOrDefault(connection.Provider);
    }
}
