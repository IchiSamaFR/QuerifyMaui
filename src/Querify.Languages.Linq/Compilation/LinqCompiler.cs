using Microsoft.EntityFrameworkCore;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.Analysis;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Compilation;
using Quill.Code.CSharp.Workspaces;
using Quill.Code.Scripting;
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
    // Un service par modele (libere avec lui) : references et contexte genere ne changent pas d'une frappe a l'autre,
    // Roslyn reutilise sa compilation.
    private static readonly ConditionalWeakTable<LinqModelAssembly, ICSharpLanguageService> ByModel = new();
    private static readonly Lazy<ICSharpLanguageService> WithoutModel = new(() => CreateLanguageService(null));

    /// <summary>Service C# (Quill) partage par l'editeur et l'execution : la requete est un fichier C# complet.</summary>
    /// <param name="model">DbContext compile de la connexion, ou null : C# seul (fautes de syntaxe uniquement).</param>
    public static ICSharpLanguageService GetLanguageService(LinqModelAssembly? model)
        => model is null ? WithoutModel.Value : ByModel.GetValue(model, static m => CreateLanguageService(m));

    /// <summary>Compile et charge le modele genere ; leve <see cref="LinqCompilationException"/> en cas d'echec.</summary>
    public static async Task<LinqModelAssembly> CompileModelAsync(EfGeneratedModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Projet a part, sans editeur : le modele est compile seul, une fois par schema.
        using var service = new CSharpLanguageService($"EfModel_{model.ContextClassName}", () => new CSharpProjectOptions
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
            throw new LinqCompilationException($"Le modele genere {model.ContextClassName} ne compile pas.", compiled.Errors.Select(e => e.Describe(model.Source)).ToArray());

        var contextType = assembly.GetType($"{model.Namespace}.{model.ContextClassName}", throwOnError: true)!;
        var extensionsType = assembly.GetType($"{model.Namespace}.{EfModelGenerator.ExtensionsClassName}", throwOnError: true)!;

        return new LinqModelAssembly
        {
            Model = model,
            Assembly = assembly,
            ContextType = contextType,
            DumpSinkProperty = extensionsType.GetProperty(EfModelGenerator.DumpSinkPropertyName, BindingFlags.NonPublic | BindingFlags.Static)!,
            Reference = compiled.Reference!
        };
    }

    /// <summary>
    /// Compile une requete (classe UserQuery complete) depuis le projet que l'editeur analyse ; les erreurs sont
    /// rapportees, pas levees. Les <paramref name="parameters"/> sont passes par leur nom aux parametres de Run.
    /// </summary>
    /// <param name="log">Recoit les Log.* de la requete (runtime de script de Quill).</param>
    public static async Task<LinqCompiledQuery> CompileQueryAsync(LinqModelAssembly model, string query, IReadOnlyList<LinqParameter>? parameters = null, Action<LogLevel, string>? log = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(query);

        // Projet meme de l'editeur (references, contexte genere) : ce qui est souligne est ce qui echoue.
        var compiled = await GetLanguageService(model).CompileAsync(query, new CSharpCompileOptions
        {
            AssemblyName = $"LinqQuery_{Guid.NewGuid():N}",

            // Le modele est servi tel quel (EF garde ses types en cache), le reste vient du runtime.
            ResolveAssembly = name => AssemblyName.ReferenceMatchesDefinition(name, model.Assembly.GetName()) ? model.Assembly : null,
            Log = log,
        }, cancellationToken);

        if (compiled.Assembly is not { } assembly)
        {
            compiled.Dispose();
            return new LinqCompiledQuery(compiled.Errors);
        }

        // Point d'entree absent ou parametre sans valeur : rapporte comme une erreur de compilation, en tete du texte.
        if (!LinqEntryPoint.TryBind(assembly, model.ContextType, parameters ?? [], out var entryPoint, out var error))
        {
            compiled.Dispose();
            return new LinqCompiledQuery([new CodeDiagnostic(0, 0, CodeDiagnosticSeverity.Error, error, LinqEntryPoint.DiagnosticCode)]);
        }

        return new LinqCompiledQuery(model, compiled, entryPoint);
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
    private static CSharpLanguageService CreateLanguageService(LinqModelAssembly? model)
        => new($"Linq_{model?.Model.ContextClassName ?? "SansConnexion"}", () => new CSharpProjectOptions
        {
            References = model is null ? LinqQuerySource.PlatformReferences : [.. LinqQuerySource.PlatformReferences, model.Reference],
            CompilationOptions = LinqQuerySource.CompilationOptions,
            ParseOptions = LinqQuerySource.ParseOptions,
            DocumentName = "requete.cs",

            // Usings implicites et alias QueryDbContext : fixes, Roslyn ne les reanalyse pas a chaque frappe.
            // Dump vient du modele, Log et Script du runtime de Quill.
            AdditionalDocuments = new Dictionary<string, string> { ["GlobalUsings.cs"] = LinqQuerySource.BuildGlobalUsings(model?.Model) },
            IncludeScriptRuntime = true,

            // Sans DbContext, les tables n'existent pas : seules les fautes de syntaxe ont un sens.
            IncludeSemanticDiagnostics = model is not null,
        });
}
