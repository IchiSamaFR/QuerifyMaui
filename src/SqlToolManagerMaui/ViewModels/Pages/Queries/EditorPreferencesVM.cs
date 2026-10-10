using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Enums.Settings;
using SqlToolManagerMaui.Core.Ioc;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Services.Settings;
using System.ComponentModel;

namespace SqlToolManagerMaui.ViewModels.Pages.Queries;

/// <summary>Fichiers recents et options d'affichage de la page Requete, conserves d'une session a l'autre.</summary>
[AutoRegister(Lifetime = ServiceLifetime.Transient)]
public partial class EditorPreferencesVM : ObservableObject
{
    private readonly IUserPreferencesStore _preferences;

    // Fourni par Attach : QueryVM recoit ce VM par injection avant de pouvoir se passer en hote.
    private IQueryWorkspace _workspace = null!;

    private bool _isLoaded;

    public EditorPreferencesVM(IUserPreferencesStore preferences)
    {
        _preferences = preferences;
    }

    /// <summary>Options d'edition liees au CodeEditor ; valeurs par defaut remplacees au chargement.</summary>
    public EditorConfiguration EditorConfiguration { get; } = EditorConfiguration.CreateDefault();

    /// <summary>Derniers fichiers ouverts ou enregistres, du plus recent au plus ancien.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<string> RecentFiles { get; set; } = [];

    /// <summary>Volet "Explorateur d'objets" ouvert.</summary>
    [ObservableProperty]
    public partial bool IsObjectExplorerOpen { get; set; } = true;

    /// <summary>Branche le VM sur la page hote ; appele une seule fois, par QueryVM.</summary>
    public void Attach(IQueryWorkspace workspace) => _workspace = workspace;

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
            _workspace.StartAsBusy(token => UpdateAsync(p => p.Panels.SetOpen(PanelKind.ObjectExplorer, value), token), blocking: false);
    }

    private void OnEditorConfigurationChanged(object? sender, PropertyChangedEventArgs e)
    {
        var editor = EditorConfiguration.Clone();
        _workspace.StartAsBusy(token => UpdateAsync(p => p.Editor = editor, token), blocking: false);
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
            _workspace.Status.Set($"Préférences non enregistrées : {ex.Message}", isError: true);
        }
    }

    // ---------- Commandes ----------

    [RelayCommand]
    private Task ClearRecentFilesAsync()
        => _workspace.RunAsBusyAsync(token => UpdateAsync(p => p.RecentFiles.Clear(), token));

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
