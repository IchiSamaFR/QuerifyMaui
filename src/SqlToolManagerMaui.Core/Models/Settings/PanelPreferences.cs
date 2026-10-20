using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Core.Models.Settings;

/// <summary>Etat ouvert / ferme des volets.</summary>
public partial class PanelPreferences : ObservableObject
{
    [ObservableProperty]
    public partial bool IsObjectExplorerOpen { get; set; } = true;

    [ObservableProperty]
    public partial bool IsDashboardParamsOpen { get; set; } = true;
}
