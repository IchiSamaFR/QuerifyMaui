namespace SqlToolManagerMaui.Models.Editors.Highlighting;

/// <summary>
/// Definition de coloration d'un langage, chargee depuis un fichier .xshd.
/// Les regles sont evaluees dans l'ordre du fichier : la premiere qui reconnait
/// quelque chose gagne, comme dans AvalonEdit.
/// </summary>
public sealed class SyntaxDefinition
{
    public required string Name { get; init; }

    /// <summary>Extensions associees au langage (ex. ".sql"), telles que declarees dans le .xshd.</summary>
    public required IReadOnlyList<string> Extensions { get; init; }

    public required IReadOnlyList<SyntaxRule> Rules { get; init; }
}
