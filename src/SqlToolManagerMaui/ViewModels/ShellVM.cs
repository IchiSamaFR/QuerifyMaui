using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Views.Pages;

namespace SqlToolManagerMaui.ViewModels;

/// <summary>
/// Etat de la navigation principale (rail lateral facon Windows 11 NavigationView).
/// </summary>
[AutoRegister]
public partial class ShellVM : ObservableObject
{
    public const double ExpandedPaneWidth = 260d;
    public const double CompactPaneWidth = 48d;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaneWidth))]
    public partial bool IsPaneOpen { get; set; } = true;

    /// <summary>Largeur du volet : etendu (libelles visibles) ou compact (icones seules).</summary>
    public double PaneWidth => IsPaneOpen ? ExpandedPaneWidth : CompactPaneWidth;

    /// <summary>Route de la page affichee, utilisee pour l'etat selectionne des items.</summary>
    [ObservableProperty]
    public partial string CurrentRoute { get; set; } = QueryPage.PAGE_ROUTE;

    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;

    [RelayCommand]
    private async Task NavigateAsync(string? route)
    {
        if (string.IsNullOrWhiteSpace(route) || route == CurrentRoute)
            return;

        await Shell.Current.GoToAsync($"//{route}");
        CurrentRoute = route;
    }
}
