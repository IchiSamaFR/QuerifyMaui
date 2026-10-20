using SqlToolManagerMaui.Core.Models.Settings;

namespace SqlToolManagerMaui.Services.Settings;

/// <summary>Persistance des preferences utilisateur (preferences.json dans AppDataDirectory).</summary>
public interface IUserPreferencesStore
{
    /// <summary>Instance unique partagee par les VM ; enregistree a chaque modification une fois chargee.</summary>
    UserPreferences Preferences { get; }

    /// <summary>Remplit <see cref="Preferences"/> depuis le fichier ; sans effet apres le premier appel.</summary>
    Task LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Ecrit tout de suite une modification en attente ; synchrone car appele pendant la fermeture de la fenetre.</summary>
    void Flush();
}
