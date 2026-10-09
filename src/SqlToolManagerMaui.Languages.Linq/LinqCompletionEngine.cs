using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using SqlToolManagerMaui.Core.Ioc;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace SqlToolManagerMaui.Languages.Linq;

/// <summary>Suggestion C# brute, independante de l'interface.</summary>
/// <param name="Label">Texte affiche (ex. "Where&lt;&gt;").</param>
/// <param name="InsertText">Texte insere (ex. "Where").</param>
/// <param name="Kind">Nature Roslyn principale : Method, Property, Class, Keyword, Local...</param>
/// <param name="Detail">Precision courte fournie par Roslyn (espace de noms d'un type...).</param>
public sealed record LinqCompletionCandidate(string Label, string InsertText, string Kind, string? Detail);

/// <summary>IntelliSense C# de Roslyn sur le texte d'une requete LINQ, contexte genere compris.</summary>
public interface ILinqCompletionEngine
{
    /// <param name="model">DbContext compile de la connexion, ou null : completion C# seule.</param>
    Task<IReadOnlyList<LinqCompletionCandidate>> GetCompletionsAsync(LinqModelAssembly? model, string query, int caretOffset, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ILinqCompletionEngine"/>
[AutoRegister(As = typeof(ILinqCompletionEngine))]
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : composition MEF de Roslyn assumee.")]
public sealed class LinqCompletionEngine : ILinqCompletionEngine
{
    /// <summary>
    /// Services Roslyn (completion comprise) : la composition MEF coute une a deux secondes,
    /// elle est faite une fois pour l'application.
    /// </summary>
    private static readonly Lazy<MefHostServices> Host = new(() => MefHostServices.Create(
        MefHostServices.DefaultAssemblies
            .Concat([typeof(RoslynCompletion.CompletionService).Assembly, Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features")])
            .Distinct()));

    /// <summary>Tags Roslyn retenus pour la nature, du plus precis au plus general.</summary>
    private static readonly string[] KindTags =
    [
        "ExtensionMethod", "Method", "Property", "Field", "Event", "Local", "Parameter", "RangeVariable",
        "Class", "Structure", "Interface", "Enum", "EnumMember", "Delegate", "Namespace", "Keyword", "Snippet"
    ];

    /// <summary>Un projet Roslyn par modele : references et contexte genere ne changent pas d'une frappe a l'autre.</summary>
    private readonly ConditionalWeakTable<LinqModelAssembly, Lazy<DocumentId>> _documents = new();
    private readonly Lazy<DocumentId> _documentWithoutModel;
    private readonly AdhocWorkspace _workspace;
    private readonly Lock _gate = new();

    public LinqCompletionEngine()
    {
        _workspace = new AdhocWorkspace(Host.Value);
        _documentWithoutModel = new Lazy<DocumentId>(() => AddDocument(null));
    }

    public async Task<IReadOnlyList<LinqCompletionCandidate>> GetCompletionsAsync(LinqModelAssembly? model, string query, int caretOffset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var documentId = model is null ? _documentWithoutModel.Value : _documents.GetValue(model, m => new Lazy<DocumentId>(() => AddDocument(m))).Value;
        var (source, queryStart) = LinqCompiler.BuildQuerySource(model?.Model, query);

        // WithText cree une version de la solution : le workspace partage n'est jamais modifie.
        var document = _workspace.CurrentSolution.GetDocument(documentId)!.WithText(SourceText.From(source));
        if (RoslynCompletion.CompletionService.GetService(document) is not { } service)
            return [];

        var position = queryStart + Math.Clamp(caretOffset, 0, query.Length);
        var list = await service.GetCompletionsAsync(document, position, cancellationToken: cancellationToken);

        return list.ItemsList
            .Select(item => new LinqCompletionCandidate(
                item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix,
                item.DisplayText,
                KindOf(item.Tags),
                string.IsNullOrEmpty(item.InlineDescription) ? null : item.InlineDescription))
            .ToArray();
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
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
                metadataReferences: references);

            var projectId = _workspace.AddProject(project).Id;
            var (source, _) = LinqCompiler.BuildQuerySource(model?.Model, string.Empty);
            return _workspace.AddDocument(projectId, "requete.cs", SourceText.From(source)).Id;
        }
    }

    private static string KindOf(ImmutableArray<string> tags)
        => KindTags.FirstOrDefault(tags.Contains) ?? (tags.IsDefaultOrEmpty ? "Text" : tags[0]);
}
