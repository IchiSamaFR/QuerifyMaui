using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Databases.Models.Connections;
using SqlToolManagerMaui.Databases.Models.Queries;

namespace SqlToolManagerMaui.Core.Models.Editors.Analysis;

/// <summary>Texte d'un editeur a analyser (survol, signature, erreurs), avec ce qui donne un sens a ses noms.</summary>
public sealed class CodeAnalysisRequest
{
    public required SyntaxLanguage Language { get; init; }

    public required string Text { get; init; }

    /// <summary>Position analysee (caractere survole, curseur) ; ignoree pour les diagnostics.</summary>
    public int Offset { get; init; }

    /// <summary>Connexion de l'onglet ; sans connexion ouverte, ni schema ni DbContext.</summary>
    public DatabaseConnectionInfo? Connection { get; init; }

    /// <summary>Base interrogee par le texte ; null = base de la connexion.</summary>
    public string? Database { get; init; }

    /// <summary>Parametres fournis a l'execution (dashboard).</summary>
    public IReadOnlyList<QueryParameter> Parameters { get; init; } = [];
}
