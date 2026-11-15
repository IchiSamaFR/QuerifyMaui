namespace Querify.Core.Models.Dashboards;

/// <summary>
/// Contenu d'un fichier .schx : parametres d'entree, et pages composees de cartes sur une grille.
/// Le fichier ne porte jamais de chaine de connexion, seulement le nom d'une connexion enregistree.
/// </summary>
public sealed class DashboardDefinition
{
    public const int CURRENT_VERSION = 1;
    public const int DEFAULT_COLUMNS = 12;
    public const int DEFAULT_ROWS = 6;

    public int Version { get; set; } = CURRENT_VERSION;

    /// <summary>Nom de la connexion enregistree proposee a l'ouverture.</summary>
    public string? Connection { get; set; }

    /// <summary>Base interrogee ; null = base par defaut de la connexion.</summary>
    public string? Database { get; set; }

    public int Columns { get; set; } = DEFAULT_COLUMNS;

    /// <summary>Lignes affichees au minimum ; la grille s'allonge avec les cartes placees plus bas.</summary>
    public int Rows { get; set; } = DEFAULT_ROWS;

    /// <summary>Page ouverte en premier (Id) ; a defaut, la premiere.</summary>
    public string? StartPage { get; set; }

    /// <summary>Parametres communs a toutes les pages (@Nom dans les requetes).</summary>
    // set et non init : le generateur System.Text.Json remet a null une propriete init absente du JSON.
    public List<DashboardParameterDefinition> Parameters { get; set; } = [];

    // set et non init : le generateur System.Text.Json remet a null une propriete init absente du JSON.
    public List<DashboardPageDefinition> Pages { get; set; } = [];

    public DashboardPageDefinition? FindPage(string? id)
        => Pages.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Dashboard vide : une page, aucune carte.</summary>
    public static DashboardDefinition CreateEmpty(string? connection, string? database)
    {
        var page = new DashboardPageDefinition { Id = DashboardPageDefinition.NewId(), Title = "Accueil" };
        return new()
        {
            Connection = connection,
            Database = database,
            StartPage = page.Id,
            Pages = [page],
        };
    }
}
