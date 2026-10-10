using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Settings;
using SqlToolManagerMaui.Core.Models.Settings;
using System.ComponentModel;

namespace SqlToolManagerMaui.ViewModels.Pages;

/// <summary>Partie "preferences" : fichiers recents et options d'affichage conservees d'une session a l'autre.</summary>
public partial class QueryVM
{
    private bool _arePreferencesLoaded;

    /// <summary>Derniers fichiers ouverts ou enregistres, du plus recent au plus ancien.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> RecentFiles { get; set; } = [];

    /// <summary>Volet "Explorateur d'objets" ouvert ; la valeur est conservee dans les preferences.</summary>
    [ObservableProperty]
    public partial bool IsObjectExplorerOpen { get; set; } = true;

    partial void OnIsObjectExplorerOpenChanged(bool value)
    {
        // Pendant le chargement, on applique les preferences sans les reecrire.
        if (_arePreferencesLoaded)
            StartAsBusy(token => UpdatePreferencesAsync(p => p.Panels.SetOpen(PanelKind.ObjectExplorer, value), token), blocking: false);
    }

    [RelayCommand]
    private void CloseObjectExplorer() => IsObjectExplorerOpen = false;

    [RelayCommand]
    private void ToggleObjectExplorer() => IsObjectExplorerOpen = !IsObjectExplorerOpen;

    private async Task LoadPreferencesAsync()
    {
        if (_arePreferencesLoaded)
            return;

        var preferences = await _preferences.GetAsync(LifetimeToken);
        EditorConfiguration.Apply(preferences.Editor);
        RecentFiles = preferences.RecentFiles;
        IsObjectExplorerOpen = preferences.Panels.IsOpen(PanelKind.ObjectExplorer);

        // Abonnement apres l'application des valeurs : le chargement ne doit pas reecrire le fichier.
        EditorConfiguration.PropertyChanged += OnEditorConfigurationChanged;
        _arePreferencesLoaded = true;
    }

    private void OnEditorConfigurationChanged(object? sender, PropertyChangedEventArgs e)
    {
        var editor = EditorConfiguration.Clone();
        StartAsBusy(token => UpdatePreferencesAsync(p => p.Editor = editor, token), blocking: false);
    }

    private async Task UpdatePreferencesAsync(Action<UserPreferences> update, CancellationToken token)
    {
        try
        {
            var preferences = await _preferences.UpdateAsync(update, token);
            RecentFiles = preferences.RecentFiles;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Status.Set($"Préférences non enregistrées : {ex.Message}", isError: true);
        }
    }

    /// <summary>Rouvre un fichier recent ; un fichier disparu est retire de la liste.</summary>
    [RelayCommand]
    private Task OpenRecentAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.CompletedTask;

        return RunAsBusyAsync(async token =>
        {
            var content = await _files.ReadAsync(filePath, token);
            if (content is null)
            {
                await UpdatePreferencesAsync(p => p.RemoveRecentFile(filePath), token);
                Status.Set($"{Path.GetFileName(filePath)} est introuvable : retiré des fichiers récents.", isError: true);
                return;
            }

            await OpenContentAsync(content, token);
        });
    }

    [RelayCommand]
    private Task ClearRecentFilesAsync()
        => RunAsBusyAsync(token => UpdatePreferencesAsync(p => p.RecentFiles.Clear(), token));
}
