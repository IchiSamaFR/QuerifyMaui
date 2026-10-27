using Querify.Core.Ioc;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp.Workspaces;
using System.Runtime.CompilerServices;

namespace Querify.Languages.Linq.Workspaces;

/// <inheritdoc cref="ILinqWorkspace"/>
[AutoRegister(As = typeof(ILinqWorkspace))]
public sealed class LinqWorkspace : ILinqWorkspace
{
    private readonly CSharpWorkspace _workspace;
    private readonly ConditionalWeakTable<LinqModelAssembly, Lazy<CSharpProject>> _projects = new();
    private readonly Lazy<CSharpProject> _projectWithoutModel;

    public LinqWorkspace(CSharpWorkspace workspace)
    {
        _workspace = workspace;
        _projectWithoutModel = new Lazy<CSharpProject>(() => AddProject(null));
    }

    public CSharpDocument GetDocument(LinqModelAssembly? model, string query, IReadOnlyList<LinqParameter>? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(query);

        var project = model is null ? _projectWithoutModel.Value : _projects.GetValue(model, m => new Lazy<CSharpProject>(() => AddProject(m))).Value;
        var (source, queryStart) = LinqCompiler.BuildQuerySource(model?.Model, query, parameters);

        // Sans DbContext, la variable de la base n'existe pas : seules les fautes de syntaxe ont un sens.
        // La requete ne peut pas declarer de using : un type non importe s'insere avec son nom complet.
        return project.CreateDocument(source, query, queryStart) with
        {
            IncludeSemanticDiagnostics = model is not null,
            AllowImports = false,
        };
    }

    private CSharpProject AddProject(LinqModelAssembly? model)
    {
        var name = model?.Model.ContextVariableName ?? "SansConnexion";

        return _workspace.AddProject(new CSharpProjectOptions
        {
            Name = $"Linq_{name}",
            References = model is null ? LinqCompiler.PlatformReferences : [.. LinqCompiler.PlatformReferences, model.Reference],
            CompilationOptions = LinqCompiler.CompilationOptions,
            ParseOptions = LinqCompiler.ParseOptions,
            DocumentName = "requete.cs",
        });
    }
}
