using CodeEdit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.Core.Models.Settings;
using SqlToolManagerMaui.Services.Settings;
using SqlToolManagerMaui.ViewModels.Pages.Shared;

namespace SqlToolManagerMaui.ViewModels.Pages.Documents;

/// <summary>
/// VM singleton d'un type d'onglet : la vue lui pousse la session affichee (<see cref="Session"/>, null quand
/// aucun onglet de ce type n'est visible). Tout l'etat propre a un onglet vit dans la session, jamais ici.
/// </summary>
public abstract partial class DocumentVM<TSession> : ObservableObject
    where TSession : AbstractSession
{
    public DatabaseConnectionVM Connection { get; }

    public StatusBarVM Status { get; }

    public UserPreferences Preferences { get; }

    /// <summary>Session affichee, poussee par la vue.</summary>
    [ObservableProperty]
    public partial TSession? Session { get; set; }

    /// <summary>Commandes d'edition du CodeEditor de la vue, poussees par le controle (OneWayToSource).</summary>
    [ObservableProperty]
    public partial CodeEditorCommands? EditionCommands { get; set; }

    protected DocumentVM(DatabaseConnectionVM connection, StatusBarVM status, IUserPreferencesStore preferences)
    {
        Connection = connection;
        Status = status;
        Preferences = preferences.Preferences;

        Connection.SelectedConnectionChanged += (_, _) => OnConnectionChanged();
    }

    partial void OnSessionChanged(TSession? oldValue, TSession? newValue) => OnSessionSwitched(oldValue, newValue);

    /// <summary>Changement d'onglet affiche (ou plus aucun onglet de ce type).</summary>
    protected abstract void OnSessionSwitched(TSession? oldValue, TSession? newValue);

    /// <summary>Connexion active changee dans l'en-tete.</summary>
    protected abstract void OnConnectionChanged();
}
