using SqlToolManagerMaui.Core.Models.Settings;

namespace SqlToolManagerMaui.Services.Settings;

/// <summary>Persistance des preferences utilisateur (preferences.json dans AppDataDirectory).</summary>
public interface IUserPreferencesStore
{
    /// <summary>Copie des preferences courantes : la modifier n'enregistre rien.</summary>
    Task<UserPreferences> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Applique la modification puis enregistre ; retourne une copie du resultat.</summary>
    Task<UserPreferences> UpdateAsync(Action<UserPreferences> update, CancellationToken cancellationToken = default);
}
