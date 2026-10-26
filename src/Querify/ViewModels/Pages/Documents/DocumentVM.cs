using CodeEdit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using Querify.Core.Models.Editors;
using Querify.Core.Models.Settings;
using Querify.Services.Settings;
using Querify.ViewModels.Pages.Shared;

namespace Querify.ViewModels.Pages.Documents;

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

        // Aussi lors d'un changement d'onglet, qui ne leve pas SelectedConnectionChanged.
        Connection.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DatabaseConnectionVM.IsConnected))
                RefreshCanExecute();
        };
    }

    partial void OnSessionChanged(TSession? oldValue, TSession? newValue) => OnSessionSwitched(oldValue, newValue);

    /// <summary>Changement d'onglet affiche (ou plus aucun onglet de ce type).</summary>
    protected abstract void OnSessionSwitched(TSession? oldValue, TSession? newValue);

    /// <summary>L'utilisateur a change la connexion de l'onglet affiche.</summary>
    protected virtual void OnConnectionChanged()
    {
    }

    /// <summary>Les commandes qui dependent de la connexion reevaluent leur CanExecute.</summary>
    protected abstract void RefreshCanExecute();
}
