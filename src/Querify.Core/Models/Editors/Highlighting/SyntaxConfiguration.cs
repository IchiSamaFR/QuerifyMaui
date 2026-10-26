using Querify.Core.Enums.Editors.Highlighting;

namespace Querify.Core.Models.Editors.Highlighting;

/// <summary>
/// Tout ce que l'editeur doit savoir d'un langage : son identite, son prefixe de
/// commentaire de ligne et ses regles de coloration.
/// </summary>
public sealed class SyntaxConfiguration
{
    public required SyntaxLanguage Language { get; init; }

    /// <summary>Prefixe de commentaire de ligne (ex. "--" en T-SQL).</summary>
    public required string Comments { get; init; }

    /// <summary>Regles de coloration chargees depuis le .xshd du langage.</summary>
    public required SyntaxDefinition Definition { get; init; }
}
