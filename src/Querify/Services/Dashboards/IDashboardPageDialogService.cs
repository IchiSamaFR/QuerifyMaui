using Querify.Core.Models.Dashboards;

namespace Querify.Services.Dashboards;

/// <summary>Ouvre la popup « Options de la page » d'un dashboard (cle et libelle).</summary>
public interface IDashboardPageDialogService
{
    /// <summary>
    /// Affiche la popup sur la page donnee ; <paramref name="usedIds"/> sont les cles deja prises par les autres pages.
    /// </summary>
    /// <returns>La saisie, ou null si l'utilisateur a annule.</returns>
    Task<DashboardPageEdit?> EditAsync(DashboardPageDefinition page, IReadOnlyCollection<string> usedIds, CancellationToken cancellationToken = default);
}
