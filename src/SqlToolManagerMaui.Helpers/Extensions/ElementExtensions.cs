namespace SqlToolManagerMaui.Helpers.Extensions;

/// <summary>
/// Extensions de navigation dans l'arbre logique des <see cref="Element"/>.
/// </summary>
public static class ElementExtensions
{
    private const int DefaultFindDepth = 10;

    /// <summary>
    /// Recherche le premier parent de type <typeparamref name="T"/> en remontant
    /// l'arbre logique, sur au plus <paramref name="maxLevels"/> niveaux.
    /// </summary>
    /// <typeparam name="T">Type de parent recherché.</typeparam>
    /// <param name="element">Élément de départ (non inclus dans la recherche).</param>
    /// <param name="maxLevels">Nombre maximum de niveaux à remonter (par défaut : 10).</param>
    /// <returns>Le parent trouvé, ou <c>null</c>.</returns>
    public static T? FindParent<T>(this Element? element, int maxLevels = DefaultFindDepth)
        where T : Element
    {
        if (element is null || maxLevels < 1)
            return null;

        var current = element.Parent;
        for (var level = 0; current is not null && level < maxLevels; level++)
        {
            if (current is T match)
                return match;

            current = current.Parent;
        }

        return null;
    }
}
