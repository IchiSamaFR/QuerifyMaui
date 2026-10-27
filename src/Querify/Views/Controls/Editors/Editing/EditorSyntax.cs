using Querify.Core.Enums.Editors.Highlighting;
using Quill.Code.Editing;

namespace Querify.Views.Controls.Editors.Editing;

/// <summary>Regles de commentaires et de chaines (Quill) du langage d'un onglet.</summary>
internal static class EditorSyntax
{
    /// <summary>T-SQL pour le SQL, C# sinon (LINQ) ; <paramref name="lineComment"/> remplace le prefixe s'il est donne.</summary>
    public static SourceSyntax For(SyntaxLanguage language, string? lineComment = null)
    {
        var syntax = language == SyntaxLanguage.Sql ? SourceSyntax.Sql : SourceSyntax.CSharp;
        return string.IsNullOrEmpty(lineComment) ? syntax : syntax with { LineComment = lineComment };
    }
}
