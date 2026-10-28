using Microsoft.EntityFrameworkCore;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.Analysis;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Workspaces;
using Quill.Code.Scripting;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Querify.Languages.Linq.Compilation;

/// <summary>
/// Requetes LINQ via Quill : services C# de l'editeur, DbContext genere (une fois par schema), puis chaque requete,
/// compilee depuis le projet meme de l'editeur. Sans etat propre hormis le cache des services.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "App non publiee en AOT : compilation et reflexion assumees.")]
public static class LinqCompiler
{
    // Un service par modele (libere avec lui) puis par jeu de parametres : references et contexte genere ne changent
    // pas d'une frappe a l'autre, Roslyn reutilise sa compilation.
    private static readonly ConditionalWeakTable<LinqModelAssembly, ConcurrentDictionary<string, ICSharpLanguageService>> ByModel = new();
    private static readonly ConcurrentDictionary<string, ICSharpLanguageService> WithoutModel = new(StringComparer.Ordinal);

    /// <summary>
    /// Service C# (Quill) partage par l'editeur et l'execution : son projet place la requete dans le contexte genere.
    /// </summary>
    /// <param name="model">DbContext compile de la connexion, ou null : C# seul (fautes de syntaxe uniquement).</param>
    /// <param name="parameters">Variables declarees avant la requete (parametres du dashboard).</param>
    public static ICSharpLanguageService GetLanguageService(LinqModelAssembly? model, IReadOnlyList<LinqParameter>? parameters = null)
    {
        parameters ??= [];

        var services = model is null ? WithoutModel : ByModel.GetValue(model, static _ => new(StringComparer.Ordinal));

        // Seules les declarations comptent dans le source genere : les valeurs ne sont fournies qu'a l'execution.
        var key = string.Join('|', parameters.Select(p => $"{p.Name}:{p.Type.FullName}"));
        return services.GetOrAdd(key, _ => CreateLanguageService(model, parameters));
    }

    /// <summary>Compile et charge le modele genere ; leve <see cref="LinqCompilationException"/> en cas d'echec.</summary>
    public static async Task<LinqModelAssembly> CompileModelAsync(EfGeneratedModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Projet a part, sans editeur : le modele est compile seul, une fois par schema.
        using var service = new CSharpLanguageService($"EfModel_{model.ContextVariableName}", () => new CSharpProjectOptions
        {
            References = LinqQuerySource.PlatformReferences,
            CompilationOptions = LinqQuerySource.CompilationOptions,
            ParseOptions = LinqQuerySource.ParseOptions,
        });

        // Non collectable : le modele vit tant que l'application, et EF garde ses types en cache.
        var compiled = await service.CompileAsync(model.Source, new CSharpCompileOptions
        {
            AssemblyName = $"EfModel_{Guid.NewGuid():N}",
            Collectible = false,
        }, cancellationToken);

        if (compiled.Assembly is not { } assembly)
            throw new LinqCompilationException($"Le modele genere pour {model.ContextVariableName} ne compile pas.", compiled.Errors.Select(e => e.Describe(model.Source)).ToArray());

        var contextType = assembly.GetType($"{model.Namespace}.{model.ContextClassName}", throwOnError: true)!;
        var extensionsType = assembly.GetType($"{model.Namespace}.{EfModelGenerator.ExtensionsClassName}", throwOnError: true)!;

        return new LinqModelAssembly
        {
            Model = model,
            Assembly = assembly,
            ContextType = contextType,
            ContextProperty = contextType.GetProperty(EfModelGenerator.ContextPropertyName, BindingFlags.Public | BindingFlags.Static)!,
            DumpSinkProperty = extensionsType.GetProperty(EfModelGenerator.DumpSinkPropertyName, BindingFlags.NonPublic | BindingFlags.Static)!,
            Reference = compiled.Reference!
        };
    }

    /// <summary>
    /// Compile une requete depuis le projet que l'editeur analyse ; les erreurs sont rapportees, pas levees.
    /// Les <paramref name="parameters"/> sont declares en variables avant la requete, leurs valeurs fixees pour l'execution.
    /// </summary>
    /// <param name="log">Recoit les Log.* de la requete (runtime de script de Quill).</param>
    public static async Task<LinqCompiledQuery> CompileQueryAsync(LinqModelAssembly model, string query, IReadOnlyList<LinqParameter>? parameters = null, Action<LogLevel, string>? log = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(query);

        parameters ??= [];

        // Projet meme de l'editeur (references, contexte genere) : ce qui est souligne est ce qui echoue.
        var compiled = await GetLanguageService(model, parameters).CompileAsync(query, new CSharpCompileOptions
        {
            AssemblyName = $"LinqQuery_{Guid.NewGuid():N}",

            // Le modele est servi tel quel (EF garde ses types en cache), le reste vient du runtime.
            ResolveAssembly = name => AssemblyName.ReferenceMatchesDefinition(name, model.Assembly.GetName()) ? model.Assembly : null,
            Log = log,
        }, cancellationToken);

        if (compiled.Assembly is not { } assembly)
        {
            compiled.Dispose();
            return new LinqCompiledQuery { Errors = compiled.Errors };
        }

        var method = assembly.GetType($"{LinqQuerySource.QueryNamespace}.{LinqQuerySource.QueryClass}", throwOnError: true)!.GetMethod(LinqQuerySource.QueryMethod)!;
        var run = method.CreateDelegate<Func<object?[], object?>>();
        var values = parameters.Select(p => p.Value).ToArray();

        return new LinqCompiledQuery
        {
            // Proprietes du modele en AsyncLocal : propres a cette execution, meme si d'autres tournent sur le meme modele.
            Execute = (context, dump) =>
            {
                model.ContextProperty.SetValue(null, context);
                model.DumpSinkProperty.SetValue(null, dump);
                return run(values);
            },
            ReturnsValue = LinqQuerySource.ReturnsValue(query),
            Compilation = compiled,
        };
    }

    /// <summary>Contexte EF du modele sur une chaine de connexion SQL Server.</summary>
    public static DbContext CreateContext(LinqModelAssembly model, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(model.ContextType))!;
        builder.UseSqlServer(connectionString)
               // Lecture seule : pas de suivi des entites, inutile pour afficher des resultats.
               .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        return (DbContext)Activator.CreateInstance(model.ContextType, builder.Options)!;
    }

    /// <summary>Projet cree par Quill a sa premiere utilisation : le declarer ne coute rien.</summary>
    private static CSharpLanguageService CreateLanguageService(LinqModelAssembly? model, IReadOnlyList<LinqParameter> parameters)
        => new($"Linq_{model?.Model.ContextVariableName ?? "SansConnexion"}", () => new CSharpProjectOptions
        {
            References = model is null ? LinqQuerySource.PlatformReferences : [.. LinqQuerySource.PlatformReferences, model.Reference],
            CompilationOptions = LinqQuerySource.CompilationOptions,
            ParseOptions = LinqQuerySource.ParseOptions,
            DocumentName = "requete.cs",

            // Usings fixes : Roslyn ne les reanalyse pas a chaque frappe. Dump vient du modele, Log et Script de Quill.
            AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = LinqQuerySource.BuildGlobalUsings(model?.Model) },
            IncludeScriptRuntime = true,

            // La requete devient le corps d'une methode ou la variable de la base et les parametres sont declares.
            SourceTransform = query =>
            {
                var (source, queryStart) = LinqQuerySource.Build(model?.Model, query, parameters);
                return new CSharpSource(source, queryStart);
            },

            // Sans DbContext, la variable de la base n'existe pas : seules les fautes de syntaxe ont un sens.
            IncludeSemanticDiagnostics = model is not null,

            // La requete ne peut pas declarer de using : un type non importe s'insere avec son nom complet.
            AllowImports = false,
        });
}
