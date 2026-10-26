namespace Querify.Core.Models.Editors.Highlighting;

/// <summary>
/// Regle de coloration issue d'un fichier .xshd : elle tente de reconnaitre un token
/// a une position donnee du texte.
/// </summary>
public abstract class SyntaxRule
{
    /// <summary>Couleur du token reconnu, telle que declaree dans le .xshd (hexadecimal).</summary>
    public required string Color { get; init; }

    /// <summary>Longueur du token reconnu a cette position, ou 0 si la regle ne s'applique pas.</summary>
    public abstract int Match(string text, int startIndex);
}
