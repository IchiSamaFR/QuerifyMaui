using SqlToolManagerMaui.Core.Enums.Editors.Highlighting;
using SqlToolManagerMaui.Core.Models.Editors.Completion;
using SqlToolManagerMaui.Core.Models.Editors.Highlighting;

namespace SqlToolManagerMaui.Core.Services.Editors.Completion;

/// <summary>Analyse du texte autour du curseur ; une implementation par langage.</summary>
public interface ICompletionContextAnalyzer
{
    SyntaxLanguage Language { get; }

    CompletionContext Analyze(CompletionRequest request);
}
