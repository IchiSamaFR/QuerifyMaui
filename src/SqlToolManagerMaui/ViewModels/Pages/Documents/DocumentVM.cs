using CodeEdit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SqlToolManagerMaui.Core.Models.Editors;
using SqlToolManagerMaui.ViewModels.Pages.Shared;
using System.Windows.Input;

namespace SqlToolManagerMaui.ViewModels.Pages.Documents;

/// <summary>
/// Un onglet de la page principale : le fichier edite et ce que sa vue affiche.
/// Cree par <see cref="DocumentVMFactory"/>, libere par <see cref="Dispose"/> a la fermeture de l'onglet.
/// </summary>
public abstract partial class DocumentVM : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();

    protected DocumentVM(EditorDocument document, DatabaseConnectionVM connection, StatusBarVM status, EditorPreferencesVM preferences)
    {
        Document = document;
        Connection = connection;
        Status = status;
        Preferences = preferences;

        Connection.SelectedConnectionChanged += OnSelectedConnectionChanged;
    }

    public EditorDocument Document { get; }

    /// <summary>Connexion partagee par tous les onglets (picker de l'en-tete).</summary>
    public DatabaseConnectionVM Connection { get; }

    public StatusBarVM Status { get; }

    public EditorPreferencesVM Preferences { get; }

    /// <summary>Onglet affiche : seul l'onglet actif reagit a un changement de connexion.</summary>
    [ObservableProperty]
    public partial bool IsActive { get; set; }

    /// <summary>Commandes d'edition du CodeEditor de la vue, poussees par le controle (OneWayToSource).</summary>
    [ObservableProperty]
    public partial CodeEditorCommands? EditionCommands { get; set; }

    /// <summary>Bouton Executer de l'en-tete et menu Requete.</summary>
    public abstract IAsyncRelayCommand ExecuteCommand { get; }

    /// <summary>Bouton Arreter de l'en-tete et menu Requete.</summary>
    public abstract ICommand CancelCommand { get; }

    /// <summary>Extension proposee par « Enregistrer sous » pour un document jamais enregistre.</summary>
    public abstract string DefaultExtension { get; }

    /// <summary>Annule a la fermeture de l'onglet.</summary>
    protected CancellationToken LifetimeToken => _lifetime.Token;

    /// <summary>Appele une fois l'onglet ajoute et selectionne.</summary>
    public virtual void OnOpened() => Status.Set($"{Document.FileName} ouvert.");

    /// <summary>Ecrit dans <see cref="EditorDocument.EditorText"/> ce qui ne l'est pas encore, avant un enregistrement.</summary>
    public virtual void PrepareSave()
    {
    }

    protected virtual void OnConnectionChanged()
    {
    }

    private void OnSelectedConnectionChanged(object? sender, EventArgs e) => OnConnectionChanged();

    public virtual void Dispose()
    {
        Connection.SelectedConnectionChanged -= OnSelectedConnectionChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }
}
