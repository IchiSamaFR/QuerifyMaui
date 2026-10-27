using Querify.Core.Ioc;
using Querify.Languages.Linq.Compilation;
using Querify.Languages.Linq.Queries;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Workspaces;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Querify.Languages.Linq.Workspaces;

/// <inheritdoc cref="ILinqLanguageServices"/>
[AutoRegister(As = typeof(ILinqLanguageServices))]
public sealed class LinqLanguageServices : ILinqLanguageServices
{
    // Un service par modele (libere avec lui) puis par jeu de parametres : references et contexte genere ne changent
    // pas d'une frappe a l'autre, Roslyn reutilise sa compilation.
    private readonly ConditionalWeakTable<LinqModelAssembly, ConcurrentDictionary<string, ICSharpLanguageService>> _byModel = new();
    private readonly ConcurrentDictionary<string, ICSharpLanguageService> _withoutModel = new(StringComparer.Ordinal);

    public ICSharpLanguageService Get(LinqModelAssembly? model, IReadOnlyList<LinqParameter>? parameters = null)
    {
        parameters ??= [];

        var services = model is null ? _withoutModel : _byModel.GetValue(model, static _ => new(StringComparer.Ordinal));
        return services.GetOrAdd(KeyOf(parameters), _ => Create(model, parameters));
    }

    // Seules les declarations comptent dans le source genere : les valeurs ne sont fournies qu'a l'execution.
    private static string KeyOf(IReadOnlyList<LinqParameter> parameters)
        => string.Join('|', parameters.Select(p => $"{p.Name}:{p.Type.FullName}"));

    /// <summary>Projet cree par Quill a sa premiere utilisation : le declarer ne coute rien.</summary>
    private static CSharpLanguageService Create(LinqModelAssembly? model, IReadOnlyList<LinqParameter> parameters)
        => new($"Linq_{model?.Model.ContextVariableName ?? "SansConnexion"}", () => new CSharpProjectOptions
        {
            References = model is null ? LinqCompiler.PlatformReferences : [.. LinqCompiler.PlatformReferences, model.Reference],
            CompilationOptions = LinqCompiler.CompilationOptions,
            ParseOptions = LinqCompiler.ParseOptions,
            DocumentName = "requete.cs",

            // Usings et Dump/Console : fixes, Roslyn ne les reanalyse pas a chaque frappe.
            AdditionalDocuments = new Dictionary<string, string>
            {
                ["GlobalUsings.cs"] = LinqCompiler.BuildGlobalUsings(model?.Model),
                ["QueryRuntime.cs"] = LinqCompiler.QueryRuntimeSource,
            },

            // La requete devient le corps d'une methode ou la variable de la base et les parametres sont declares.
            SourceTransform = query =>
            {
                var (source, queryStart) = LinqCompiler.BuildQuerySource(model?.Model, query, parameters);
                return new CSharpSource(source, queryStart);
            },

            // Sans DbContext, la variable de la base n'existe pas : seules les fautes de syntaxe ont un sens.
            IncludeSemanticDiagnostics = model is not null,

            // La requete ne peut pas declarer de using : un type non importe s'insere avec son nom complet.
            AllowImports = false,
        });
}
