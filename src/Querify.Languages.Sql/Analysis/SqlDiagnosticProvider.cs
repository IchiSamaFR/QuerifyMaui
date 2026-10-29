using Querify.Core.Enums.Editors.Highlighting;
using Querify.Core.Ioc;
using Querify.Core.Models.Editors.Analysis;
using Querify.Languages.Commons.Editors.Analysis;
using Quill.Code.Analysis;
using Quill.Code.Sql;
using Quill.Code.Sql.Analysis;

namespace Querify.Languages.Sql.Analysis;

/// <summary>Fautes de syntaxe T-SQL (lots GO compris, sans connexion), calculees par Quill.Code.Sql.</summary>
[AutoRegister(As = typeof(IDiagnosticProvider))]
public sealed class SqlDiagnosticProvider : IDiagnosticProvider
{
    public Task<IReadOnlyList<CodeDiagnostic>> GetDiagnosticsAsync(CodeAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Language != SyntaxLanguage.Sql)
            return Task.FromResult<IReadOnlyList<CodeDiagnostic>>([]);

        return Task.FromResult(SqlSyntaxChecker.Check(request.Text));
    }
}
