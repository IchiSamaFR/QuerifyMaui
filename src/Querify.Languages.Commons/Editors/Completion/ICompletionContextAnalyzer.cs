using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Models.Editors.Completion;

namespace Querify.Languages.Commons.Editors.Completion;

/// <summary>Analyse du texte autour du curseur ; une implementation par langage.</summary>
public interface ICompletionContextAnalyzer
{
    SyntaxLanguage Language { get; }

    CompletionContext Analyze(CompletionRequest request);
}
