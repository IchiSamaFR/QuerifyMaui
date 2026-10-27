using Quill.Code.Analysis;
using Quill.Code.Text;

namespace Querify.Languages.Linq.Workspaces;

/// <summary>Texte des erreurs de compilation pour l'onglet Messages.</summary>
internal static class LinqDiagnosticMapper
{
    /// <summary>Ex. "Ligne 2, colonne 6 : CS1061 ..." (lignes et colonnes en base 1).</summary>
    public static string Describe(CodeDiagnostic diagnostic, string query)
    {
        var position = LinePosition.FromOffset(query, diagnostic.Start);
        return $"Ligne {position.Line}, colonne {position.Column} : {diagnostic.Code} {diagnostic.Message}";
    }
}
