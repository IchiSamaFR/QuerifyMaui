using SqlToolManagerMaui.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Services.Editors.Highlighting;

/// <summary>
/// Tokenizer a balayage unique : a chaque position, la premiere regle qui reconnait
/// quelque chose l'emporte, ce qui garantit des tokens ordonnes et disjoints.
/// </summary>
public sealed class SyntaxHighlighter : ISyntaxHighlighter
{
    public IReadOnlyList<SyntaxToken> Tokenize(SyntaxDefinition definition, string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(text);

        List<SyntaxToken> tokens = [];
        var position = 0;

        while (position < text.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var length = 0;
            foreach (var rule in definition.Rules)
            {
                length = rule.Match(text, position);
                if (length > 0)
                {
                    tokens.Add(new SyntaxToken(position, length, rule.Color));
                    break;
                }
            }

            position += length > 0 ? length : 1;
        }

        return tokens;
    }
}
