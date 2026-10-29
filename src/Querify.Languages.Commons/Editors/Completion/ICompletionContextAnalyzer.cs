using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Editors.Completion;
using Quill.Code.Sql.Completion;

namespace Querify.Languages.Commons.Editors.Completion;

/// <summary>Analyse du texte autour du curseur ; une implementation par langage.</summary>
public interface ICompletionContextAnalyzer
{
    SyntaxLanguage Language { get; }

    SqlContext Analyze(CompletionRequest request);
}
