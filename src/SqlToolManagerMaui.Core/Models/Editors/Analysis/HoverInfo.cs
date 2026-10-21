namespace SqlToolManagerMaui.Core.Models.Editors.Analysis;

/// <summary>Infobulle d'un nom survole.</summary>
/// <param name="Start">Debut du nom dans le texte : l'infobulle reste ouverte tant que la souris y est.</param>
/// <param name="Signature">Ligne principale (ex. "(propriete) string Student.Name").</param>
/// <param name="Description">Precision facultative (colonne SQL, table d'origine...).</param>
public sealed record HoverInfo(int Start, int Length, string Signature, string? Description = null)
{
    public int End => Start + Length;
}
