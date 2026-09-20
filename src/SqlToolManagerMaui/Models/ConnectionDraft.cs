using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.UI.Converters;
using SqlToolManagerMaui.UI.Extensions;

namespace SqlToolManagerMaui.Models;

/// <summary>
/// Etat d'edition du formulaire de connexion.
///
/// C'est le seul objet qui porte la chaine de connexion complete : il n'est jamais
/// serialise, contrairement a <see cref="DatabaseConnectionInfo"/> qui part en clair
/// dans connections.json. Le descripteur edite est un clone, pour qu'un abandon
/// (Annuler) ne modifie rien dans la liste.
/// </summary>
public sealed partial class ConnectionDraft : ObservableObject
{
    /// <summary>Chaine telle que lue dans le coffre, mot de passe compris.</summary>
    private readonly string _stored;

    /// <summary>Descripteur en cours d'edition (un clone de l'element selectionne).</summary>
    public DatabaseConnectionInfo Info { get; }

    /// <summary>Chaine affichee dans le formulaire : le mot de passe y est masque.</summary>
    [ObservableProperty]
    public partial string ConnectionString { get; set; } = string.Empty;

    /// <summary>Faux quand le coffre n'a rendu aucune chaine pour cette connexion.</summary>
    public bool HasStoredConnectionString => !string.IsNullOrEmpty(_stored);

    private ConnectionDraft(DatabaseConnectionInfo info, string stored)
    {
        Info = info;
        _stored = stored;
        ConnectionString = ConnectionStringMaskerConverter.Mask(stored);
    }

    /// <summary>Formulaire de creation : descripteur vierge, aucune chaine.</summary>
    public static ConnectionDraft ForNew() => new(
        new DatabaseConnectionInfo { Provider = DatabaseProvider.SqlServer },
        string.Empty);

    /// <summary>Formulaire d'edition : le mot de passe arrive deja masque.</summary>
    public static ConnectionDraft ForExisting(DatabaseConnectionInfo info, string? stored)
        => new(info.Clone(), stored ?? string.Empty);

    /// <summary>
    /// Chaine reellement utilisee pour enregistrer ou tester : tant que le masque est en
    /// place, il vaut "mot de passe inchange" et l'ancienne valeur est reinjectee. Un mot
    /// de passe ressaisi remplace le masque, donc gagne.
    /// </summary>
    public string ResolveConnectionString()
        => ConnectionStringMaskerConverter.Unmask(ConnectionString?.Trim() ?? string.Empty, _stored);
}
