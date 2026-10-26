using Querify.Core.Models.Editors.Highlighting;

namespace Querify.Services.Editors.Highlighting;

/// <summary>Decoupe un texte en tokens colores a partir d'une definition de langage.</summary>
public interface ISyntaxHighlighter
{
    /// <summary>
    /// Tokens ordonnes et sans chevauchement, comme l'exige une couche de decorations CodeEdit.
    /// </summary>
    IReadOnlyList<SyntaxToken> Tokenize(SyntaxDefinition definition, string text, CancellationToken cancellationToken = default);
}
