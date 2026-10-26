using Querify.Core.Enums.Editors.Highlighting;

namespace Querify.ViewModels.Pages.Documents;

/// <summary>Langages proposes par les listes deroulantes des en-tetes (script et carte de dashboard).</summary>
public static class QueryLanguages
{
    /// <summary>La cle est le langage, la valeur son libelle.</summary>
    public static Dictionary<SyntaxLanguage, string> All { get; } = new()
    {
        [SyntaxLanguage.Sql] = "SQL",
        [SyntaxLanguage.Linq] = "LINQ (EF)",
    };
}
