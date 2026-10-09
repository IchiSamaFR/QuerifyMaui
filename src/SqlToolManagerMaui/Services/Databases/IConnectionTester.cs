using Microsoft.EntityFrameworkCore.Storage;
using SqlToolManagerMaui.Core.Enums.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Services.Databases.Connections;

namespace SqlToolManagerMaui.Services.Databases;

/// <summary>
/// Validation syntaxique et test d'ouverture d'une chaine de connexion.
/// Une implementation par moteur, resolue par <see cref="Provider"/>.
/// </summary>
public interface IConnectionTester
{
    DatabaseProvider Provider { get; }

    /// <summary>
    /// Analyse la chaine sans ouvrir de connexion. Renseigne serveur et base sur
    /// <paramref name="info"/> quand l'analyse reussit.
    /// </summary>
    /// <returns>Le message d'erreur, ou null si la chaine est valide.</returns>
    string? Validate(string connectionString, DatabaseConnectionInfo info);

    /// <summary>Ouvre reellement la connexion puis la referme.</summary>
    Task<ConnectionTestResult> TestAsync(string connectionString, CancellationToken cancellationToken = default);
}
