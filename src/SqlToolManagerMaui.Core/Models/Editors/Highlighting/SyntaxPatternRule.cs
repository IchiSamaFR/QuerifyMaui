using System.Text.RegularExpressions;

namespace SqlToolManagerMaui.Core.Models.Editors.Highlighting;

/// <summary>
/// Regle a expression reguliere : sert aussi bien aux &lt;Rule&gt; qu'aux listes
/// de &lt;Keywords&gt;, ces dernieres etant compilees en une alternation.
/// </summary>
public sealed class SyntaxPatternRule : SyntaxRule
{
    private readonly Regex _pattern;

    public SyntaxPatternRule(Regex pattern)
    {
        _pattern = pattern;
    }

    public override int Match(string text, int startIndex)
    {
        var match = _pattern.Match(text, startIndex);

        // Le motif est ancre par \G, mais on revalide : une regle .xshd peut en ajouter un autre.
        return match.Success && match.Index == startIndex ? match.Length : 0;
    }
}
