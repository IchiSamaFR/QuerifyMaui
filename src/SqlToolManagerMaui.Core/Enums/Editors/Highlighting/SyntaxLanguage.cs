namespace SqlToolManagerMaui.Core.Enums.Editors.Highlighting;

/// <summary>
/// Langages colores par l'editeur. Chaque valeur correspond a un fichier
/// Resources/Raw/SyntaxHighlights/&lt;valeur&gt;.xshd : ajouter un langage, c'est
/// ajouter une valeur ici et le fichier du meme nom.
/// </summary>
public enum SyntaxLanguage
{
    /// <summary>Aucune coloration.</summary>
    None = 0,

    /// <summary>T-SQL (Sql.xshd).</summary>
    Sql,

    /// <summary>Requete LINQ en C# sur un DbContext Entity Framework genere (Linq.xshd).</summary>
    Linq,

    /// <summary>Script JavaScript execute par Jint, avec l'objet db (JavaScript.xshd).</summary>
    JavaScript,

    /// <summary>Script TypeScript : types retires puis execute comme du JavaScript (TypeScript.xshd).</summary>
    TypeScript
}
