namespace SqlToolManagerMaui.Models.Editors.Highlighting;

/// <summary>
/// Regle de coloration issue d'un fichier .xshd : elle tente de reconnaitre un token
/// a une position donnee du texte.
/// </summary>
public abstract class SyntaxRule
{
    /// <summary>Couleur appliquee au token reconnu.</summary>
    public required Color Color { get; init; }

    /// <summary>Longueur du token reconnu a cette position, ou 0 si la regle ne s'applique pas.</summary>
    public abstract int Match(string text, int startIndex);
}
