using SqlToolManagerMaui.Models.Editors.Completion;
using SqlToolManagerMaui.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Services.Editors.Completion;

/// <summary>Analyse du texte autour du curseur ; une implementation par langage.</summary>
public interface ICompletionContextAnalyzer
{
    SyntaxLanguage Language { get; }

    CompletionContext Analyze(CompletionRequest request);
}
