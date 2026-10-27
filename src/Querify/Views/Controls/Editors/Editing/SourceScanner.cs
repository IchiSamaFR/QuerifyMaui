using Querify.Core.Enums.Editors;
using Querify.Core.Enums.Editors.Highlighting;

namespace Querify.Views.Controls.Editors.Editing;

/// <summary>
/// Decoupe minimal du source en code / commentaires / chaines, sans coloration : T-SQL ('...' avec '' double,
/// -- et /* */), sinon C# (// et /* */, "..." et '...' avec \, @"..." avec "" double).
/// </summary>
internal static class SourceScanner
{
    /// <summary>Region de chaque caractere du texte.</summary>
    public static SourceRegion[] Classify(string text, SyntaxLanguage language)
    {
        var regions = new SourceRegion[text.Length];
        Scan(text, text.Length, language, regions);
        return regions;
    }

    /// <summary>Region dans laquelle tombe une insertion a <paramref name="offset"/>.</summary>
    public static SourceRegion RegionAt(string text, int offset, SyntaxLanguage language)
        => Scan(text, Math.Clamp(offset, 0, text.Length), language, null);

    /// <returns>La region encore ouverte apres text[..limit].</returns>
    private static SourceRegion Scan(string text, int limit, SyntaxLanguage language, SourceRegion[]? regions)
    {
        var isSql = language == SyntaxLanguage.Sql;
        var state = SourceRegion.Code;
        var quote = '\0';
        var isVerbatim = false;

        var i = 0;
        while (i < limit)
        {
            var c = text[i];
            var next = i + 1 < limit ? text[i + 1] : '\0';

            // Region des caracteres consommes : un delimiteur fermant appartient encore a sa region.
            var region = state;
            var length = 1;

            switch (state)
            {
                case SourceRegion.Code:
                    if (isSql ? c == '-' && next == '-' : c == '/' && next == '/')
                    {
                        state = region = SourceRegion.LineComment;
                        length = 2;
                    }
                    else if (c == '/' && next == '*')
                    {
                        state = region = SourceRegion.BlockComment;
                        length = 2;
                    }
                    else if (c is '\'' or '"')
                    {
                        state = region = SourceRegion.String;
                        quote = c;
                        isVerbatim = !isSql && c == '"' && i > 0 && text[i - 1] == '@';
                    }
                    break;

                case SourceRegion.LineComment:
                    if (c == '\n')
                        state = region = SourceRegion.Code;
                    break;

                case SourceRegion.BlockComment:
                    if (c == '*' && next == '/')
                    {
                        length = 2;
                        state = SourceRegion.Code;
                    }
                    break;

                case SourceRegion.String:
                    if (c == quote)
                    {
                        if ((isSql || isVerbatim) && next == quote)
                            length = 2;
                        else
                            state = SourceRegion.Code;
                    }
                    else if (c == '\\' && !isSql && !isVerbatim)
                    {
                        length = 2;
                    }
                    else if (c == '\n' && !isSql && !isVerbatim)
                    {
                        // Chaine C# non terminee : elle s'arrete a la ligne.
                        state = region = SourceRegion.Code;
                    }
                    break;
            }

            length = Math.Min(length, limit - i);
            regions?.AsSpan(i, length).Fill(region);
            i += length;
        }

        return state;
    }
}
