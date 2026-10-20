using CommunityToolkit.Mvvm.ComponentModel;

namespace SqlToolManagerMaui.Core.Models.Editors;

/// <summary>
/// Etat d'un onglet conserve d'un affichage a l'autre : le fichier edite et ce que le ViewModel
/// (singleton, partage par les onglets du meme type) doit retrouver en revenant sur l'onglet.
/// </summary>
public abstract class AbstractSession : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();

    protected AbstractSession(EditorDocument document)
    {
        Document = document;
    }

    public EditorDocument Document { get; }

    /// <summary>Connexion choisie pour cet onglet ; null = aucune. Chaque onglet garde la sienne.</summary>
    public Guid? ConnectionId { get; set; }

    /// <summary>Annule a la fermeture de l'onglet : les traitements lances pour lui s'arretent.</summary>
    public CancellationToken LifetimeToken => _lifetime.Token;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        // Pas de Dispose : un traitement encore en cours peut utiliser le jeton.
        _lifetime.Cancel();
    }
}
