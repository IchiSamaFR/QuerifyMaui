using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Completion;
using Querify.Languages.Commons.Editors.Completion;
using Quill.Code.Sql;
using Quill.Code.Sql.Completion;

namespace Querify.Languages.Sql.Completion;

/// <summary>Branche l'analyse T-SQL de Quill.Code.Sql sur le service d'autocompletion.</summary>
[AutoRegister(As = typeof(ICompletionContextAnalyzer))]
public sealed class SqlCompletionContextAnalyzer : ICompletionContextAnalyzer
{
    public SyntaxLanguage Language => SyntaxLanguage.Sql;

    public SqlContext Analyze(CompletionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SqlContextAnalyzer.Analyze(request.Text, request.CaretOffset, request.Database);
    }
}
