using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Querify.Core.Models.Settings;

/// <summary>Preferences de l'utilisateur, conservees d'une session a l'autre.</summary>
public partial class UserPreferences : ObservableObject
{

    /// <summary>Du plus recent au plus ancien ; remplacee (jamais modifiee) pour notifier les vues.</summary>
    [ObservableProperty]
    public partial List<string> RecentFiles { get; set; } = [];

    /// <summary>Dashboards (.schx) dont l'utilisateur a autorise les requetes ; remplacee (jamais modifiee) pour notifier les vues.</summary>
    [ObservableProperty]
    public partial List<string> TrustedDashboards { get; set; } = [];

    /// <summary>Derniere connexion de chaque fichier, la plus recente en tete ; remplacee (jamais modifiee) pour notifier les vues.</summary>
    [ObservableProperty]
    public partial List<FileConnection> FileConnections { get; set; } = [];

    /// <summary>Rouvrir au demarrage les onglets laisses ouverts a la fermeture (modifications des onglets sans titre comprises).</summary>
    [ObservableProperty]
    public partial bool RestoreOpenTabs { get; set; } = true;

    // Lecture seule : instances stables pour les bindings ; Json.NET les remplit en place.
    public EditorPreferences Editor { get; } = new();

    public PanelPreferences Panels { get; } = new();

    /// <summary>Leve a chaque modification (y compris de Editor et Panels) ; evenement, donc jamais serialise.</summary>
    public event Action? PreferencesChanged;

    public UserPreferences()
    {
        PropertyChanged += OnPropertyChangedHandlers;
        Editor.PropertyChanged += OnPropertyChangedHandlers;
        Panels.PropertyChanged += OnPropertyChangedHandlers;
    }

    private void OnPropertyChangedHandlers(object? sender, PropertyChangedEventArgs e)
    {
        PreferencesChanged?.Invoke();
    }
}
