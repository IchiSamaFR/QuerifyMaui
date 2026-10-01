using SqlToolManagerMaui.Models.Databases;

namespace SqlToolManagerMaui.Services.Databases;

/// <summary>Ouvre la popup "Se connecter" (creation ou modification d'une connexion).</summary>
public interface IConnectionDialogService
{
    /// <summary>
    /// Affiche la popup sur la connexion donnee, ou sur une connexion vierge si null.
    /// </summary>
    /// <returns>L'Id de la connexion enregistree, ou null si l'utilisateur a annule.</returns>
    Task<Guid?> EditAsync(DatabaseConnectionInfo? connection, CancellationToken cancellationToken = default);
}
