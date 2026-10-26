using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Querify.Core.Ioc;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Querify.Languages.Linq.Workspaces;

/// <inheritdoc cref="ILinqWorkspace"/>
[AutoRegister(As = typeof(ILinqWorkspace))]
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : composition MEF de Roslyn assumee.")]
public sealed class LinqWorkspace : ILinqWorkspace
{
    /// <summary>
    /// Services Roslyn (completion comprise) : la composition MEF coute une a deux secondes,
    /// elle est faite une fois pour l'application.
    /// </summary>
    private static readonly Lazy<MefHostServices> Host = new(() => MefHostServices.Create(
        MefHostServices.DefaultAssemblies
            .Concat([typeof(RoslynCompletion.CompletionService).Assembly, Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features")])
            .Distinct()));

    /// <summary>Un projet Roslyn par modele : references et contexte genere ne changent pas d'une frappe a l'autre.</summary>
    private readonly ConditionalWeakTable<LinqModelAssembly, Lazy<DocumentId>> _documents = new();
    private readonly Lazy<DocumentId> _documentWithoutModel;
    private readonly AdhocWorkspace _workspace;
    private readonly Lock _gate = new();

    public LinqWorkspace()
    {
        _workspace = new AdhocWorkspace(Host.Value);
        _documentWithoutModel = new Lazy<DocumentId>(() => AddDocument(null));
    }

    public LinqDocument GetDocument(LinqModelAssembly? model, string query, IReadOnlyList<LinqParameter>? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(query);

        var documentId = model is null ? _documentWithoutModel.Value : _documents.GetValue(model, m => new Lazy<DocumentId>(() => AddDocument(m))).Value;
        var (source, queryStart) = LinqCompiler.BuildQuerySource(model?.Model, query, parameters);

        // WithText cree une version de la solution : le workspace partage n'est jamais modifie.
        var document = _workspace.CurrentSolution.GetDocument(documentId)!.WithText(SourceText.From(source));
        return new LinqDocument(document, queryStart, query.Length, model is not null);
    }

    private DocumentId AddDocument(LinqModelAssembly? model)
    {
        lock (_gate)
        {
            var name = model?.Model.ContextVariableName ?? "SansConnexion";
            IReadOnlyList<MetadataReference> references = model is null ? LinqCompiler.PlatformReferences : [.. LinqCompiler.PlatformReferences, model.Reference];

            var project = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Create(),
                $"Linq_{name}",
                $"Linq_{name}",
                LanguageNames.CSharp,
                compilationOptions: LinqCompiler.CompilationOptions,
                parseOptions: LinqCompiler.ParseOptions,
                metadataReferences: references);

            var projectId = _workspace.AddProject(project).Id;
            var (source, _) = LinqCompiler.BuildQuerySource(model?.Model, string.Empty);
            return _workspace.AddDocument(projectId, "requete.cs", SourceText.From(source)).Id;
        }
    }
}
