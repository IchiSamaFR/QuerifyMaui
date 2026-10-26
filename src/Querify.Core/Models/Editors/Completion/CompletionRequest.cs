using Querify.Core.Enums.Editors.Completion;
using Querify.Core.Enums.Editors.Highlighting;
using Querify.Databases.Models.Connections;
using Querify.Databases.Models.Queries;

namespace Querify.Core.Models.Editors.Completion;

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

    /// <summary>Base interrogee par le texte ; null = base de la connexion.</summary>
    public string? Database { get; init; }

    /// <summary>Parametres fournis a l'execution (dashboard) : connus de la completion avant d'etre ecrits.</summary>
    public IReadOnlyList<QueryParameter> Parameters { get; init; } = [];

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
