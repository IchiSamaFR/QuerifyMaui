using System.Text.RegularExpressions;

namespace SqlToolManagerMaui.Models.Editors.Highlighting;

/// <summary>
/// Regle &lt;Span&gt; : un debut, puis tout ce qui suit jusqu'au motif de fin
/// (chaine, commentaire, identifiant entre crochets...).
/// </summary>
public sealed class SyntaxSpanRule : SyntaxRule
{
    private readonly Regex _begin;

    /// <summary>Motif de fin, ou null quand le span s'arrete en fin de ligne (commentaire --).</summary>
    private readonly Regex? _end;

    private readonly bool _isMultiline;

    public SyntaxSpanRule(Regex begin, Regex? end, bool isMultiline)
    {
        _begin = begin;
        _end = end;
        _isMultiline = isMultiline;
    }

    public override int Match(string text, int startIndex)
    {
        var begin = _begin.Match(text, startIndex);
        if (!begin.Success || begin.Index != startIndex || begin.Length == 0)
            return 0;

        var limit = _isMultiline ? text.Length : LineEnd(text, startIndex);

        if (_end is null)
            return limit - startIndex;

        var searchStart = startIndex + begin.Length;
        if (searchStart >= limit)
            return limit - startIndex;

        var end = _end.Match(text, searchStart, limit - searchStart);

        // Span non ferme (chaine en cours de frappe) : on colore jusqu'a la limite.
        return end.Success ? end.Index + end.Length - startIndex : limit - startIndex;
    }

    private static int LineEnd(string text, int startIndex)
    {
        var index = text.IndexOf('\n', startIndex);
        return index < 0 ? text.Length : index;
    }
}
