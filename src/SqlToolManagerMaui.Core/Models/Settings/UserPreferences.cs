using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Models.Editors;
using System.ComponentModel;

namespace SqlToolManagerMaui.Core.Models.Settings;

/// <summary>Preferences de l'utilisateur, conservees d'une session a l'autre.</summary>
public partial class UserPreferences : ObservableObject
{

    /// <summary>Du plus recent au plus ancien ; remplacee (jamais modifiee) pour notifier les vues.</summary>
    [ObservableProperty]
    public partial List<string> RecentFiles { get; set; } = [];

    /// <summary>Rouvrir au demarrage les onglets laisses ouverts a la fermeture (modifications non enregistrees comprises).</summary>
    [ObservableProperty]
    public partial bool RestoreOpenTabs { get; set; } = true;

    // Lecture seule : instances stables pour les bindings ; Json.NET les remplit en place.
    public EditorPreferences Editor { get; } = new();

    public PanelPreferences Panels { get; } = new();

    public Action PreferencesChanged { get; set; }

    public UserPreferences()
    {
        PropertyChanged += OnPropertyChangedHandlers;
        Editor.PropertyChanged += OnPropertyChangedHandlers;
        Panels.PropertyChanged += OnPropertyChangedHandlers;
    }

    private void OnPropertyChangedHandlers(object sender, PropertyChangedEventArgs e)
    {
        PreferencesChanged?.Invoke();
    }
}
