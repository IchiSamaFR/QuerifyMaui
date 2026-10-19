using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Settings;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Services.Settings;
using System.ComponentModel;

namespace SqlToolManagerMaui.ViewModels.Pages.Shared;

/// <summary>Fichiers recents et options d'affichage, conserves d'une session a l'autre et partages par tous les onglets.</summary>
[AutoRegister]
public partial class EditorPreferencesVM : ObservableObject
{
    private readonly IUserPreferencesStore _preferences;
    private readonly StatusBarVM _status;

    private bool _isLoaded;

    public EditorPreferencesVM(IUserPreferencesStore preferences, StatusBarVM status)
    {
        _preferences = preferences;
        _status = status;
    }

    /// <summary>Options d'edition liees aux CodeEditor ; valeurs par defaut remplacees au chargement.</summary>
    public EditorConfiguration EditorConfiguration { get; } = EditorConfiguration.CreateDefault();

    /// <summary>Derniers fichiers ouverts ou enregistres, du plus recent au plus ancien.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> RecentFiles { get; set; } = [];

    /// <summary>Volet "Explorateur d'objets" ouvert.</summary>
    [ObservableProperty]
    public partial bool IsObjectExplorerOpen { get; set; } = true;

    public async Task LoadAsync(CancellationToken token)
    {
        if (_isLoaded)
            return;

        var preferences = await _preferences.GetAsync(token);
        EditorConfiguration.Apply(preferences.Editor);
        RecentFiles = preferences.RecentFiles;
        IsObjectExplorerOpen = preferences.Panels.IsOpen(PanelKind.ObjectExplorer);

        // Abonnement apres l'application des valeurs : le chargement ne doit pas reecrire le fichier.
        EditorConfiguration.PropertyChanged += OnEditorConfigurationChanged;
        _isLoaded = true;
    }

    public Task AddRecentFileAsync(string filePath, CancellationToken token)
        => UpdateAsync(p => p.AddRecentFile(filePath), token);

    public Task RemoveRecentFileAsync(string filePath, CancellationToken token)
        => UpdateAsync(p => p.RemoveRecentFile(filePath), token);

    partial void OnIsObjectExplorerOpenChanged(bool value)
    {
        // Pendant le chargement, on applique les preferences sans les reecrire.
        if (_isLoaded)
            _ = UpdateAsync(p => p.Panels.SetOpen(PanelKind.ObjectExplorer, value), CancellationToken.None);
    }

    private void OnEditorConfigurationChanged(object? sender, PropertyChangedEventArgs e)
    {
        var editor = EditorConfiguration.Clone();
        _ = UpdateAsync(p => p.Editor = editor, CancellationToken.None);
    }

    private async Task UpdateAsync(Action<UserPreferences> update, CancellationToken token)
    {
        try
        {
            var preferences = await _preferences.UpdateAsync(update, token);
            RecentFiles = preferences.RecentFiles;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _status.Set($"Préférences non enregistrées : {ex.Message}", isError: true);
        }
    }

    // ---------- Commandes ----------

    [RelayCommand]
    private Task ClearRecentFilesAsync() => UpdateAsync(p => p.RecentFiles.Clear(), CancellationToken.None);

    [RelayCommand]
    private void CloseObjectExplorer() => IsObjectExplorerOpen = false;

    [RelayCommand]
    private void ToggleObjectExplorer() => IsObjectExplorerOpen = !IsObjectExplorerOpen;

    [RelayCommand]
    private void ToggleLineNumbers()
        => EditorConfiguration.IsLineDisplayed = !EditorConfiguration.IsLineDisplayed;

    [RelayCommand]
    private void ToggleWordWrap()
        => EditorConfiguration.IsWordWrapped = !EditorConfiguration.IsWordWrapped;

    [RelayCommand]
    private void ToggleUseTabs()
        => EditorConfiguration.IsTabIndentation = !EditorConfiguration.IsTabIndentation;
}
