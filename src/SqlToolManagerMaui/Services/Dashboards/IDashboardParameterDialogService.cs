using SqlToolManagerMaui.Core.Models.Dashboards;

namespace SqlToolManagerMaui.Services.Dashboards;

/// <summary>Ouvre la popup de declaration d'un parametre de dashboard.</summary>
public interface IDashboardParameterDialogService
{
    /// <summary>
    /// Affiche la popup sur le parametre donne, ou sur un parametre vierge si null ;
    /// <paramref name="usedNames"/> sont les noms deja pris par les autres parametres.
    /// </summary>
    /// <returns>Une nouvelle definition portant la saisie, ou null si l'utilisateur a annule.</returns>
    Task<DashboardParameterDefinition?> EditAsync(DashboardParameterDefinition? parameter, IReadOnlyCollection<string> usedNames, CancellationToken cancellationToken = default);
}
