using Querify.Core.Enums.Editors.Completion;

namespace Querify.Core.Models.Editors.Completion;

/// <summary>Resultat de l'analyse du texte autour du curseur.</summary>
public sealed class CompletionContext
{
    public static CompletionContext None { get; } = new() { Kind = CompletionContextKind.None };

    public static CompletionContext General { get; } = new() { Kind = CompletionContextKind.General };

    public required CompletionContextKind Kind { get; init; }

    /// <summary>Parties avant le point, dans l'ordre : ["u"] pour u.|, ["db", "dbo"] pour db.dbo.|</summary>
    public IReadOnlyList<string> Qualifier { get; init; } = [];

    /// <summary>Contexte du premier mot du qualificatif : FROM dbo.| (TableSource) n'attend pas la meme chose que EXEC dbo.|</summary>
    public CompletionContextKind MemberParentKind { get; init; } = CompletionContextKind.General;

    /// <summary>Tables du lot courant, la plus proche du curseur en premier.</summary>
    public IReadOnlyList<SqlTableReference> Tables { get; init; } = [];

    /// <summary>Base du dernier USE avant le curseur, a defaut celle de la demande ; null = base de la connexion.</summary>
    public string? Database { get; init; }

    /// <summary>Table designee par un alias ou un nom simple (u.| ou Users.|).</summary>
    public SqlTableReference? FindTable(string exposedName)
        => Tables.FirstOrDefault(t => string.Equals(t.ExposedName, exposedName, StringComparison.OrdinalIgnoreCase));
}
