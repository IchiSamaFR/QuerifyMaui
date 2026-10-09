using SqlToolManagerMaui.Core.Enums.Editors.Completion;
using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Databases.Connections;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Core.Models.Editors.Completion;

/// <summary>Demande de suggestions pour la position du curseur.</summary>
public sealed class CompletionRequest
{
    public required SyntaxLanguage Language { get; init; }

    public required string Text { get; init; }

    public required int CaretOffset { get; init; }

    /// <summary>Debut du mot en cours de frappe (egal au curseur si aucun).</summary>
    public required int WordStart { get; init; }

    public required CompletionTrigger Trigger { get; init; }

    /// <summary>Connexion de l'onglet ; sans connexion ouverte, pas de suggestion du schema.</summary>
    public DatabaseConnectionInfo? Connection { get; init; }

    public string Prefix => Text[WordStart..CaretOffset];

    /// <summary>@ et # pour les variables et tables temporaires T-SQL.</summary>
    public static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';

    public static int FindWordStart(string text, int caretOffset)
    {
        var start = Math.Clamp(caretOffset, 0, text.Length);
        while (start > 0 && IsIdentifierChar(text[start - 1]))
            start--;

        return start;
    }
}
