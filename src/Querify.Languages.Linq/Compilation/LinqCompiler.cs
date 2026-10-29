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
    // Un service par modele (libere avec lui) et par jeu de parametres (noms et types, pas les valeurs) : references,
    // contexte genere et parametres ne changent pas d'une frappe a l'autre, Roslyn reutilise sa compilation.
    private static readonly ConditionalWeakTable<LinqModelAssembly, ConcurrentDictionary<string, Lazy<ICSharpLanguageService>>> ByModel = new();
    private static readonly ConcurrentDictionary<string, Lazy<ICSharpLanguageService>> WithoutModel = new();

    private const int MaxServicesPerModel = 16;

    /// <summary>Service C# (Quill) partage par l'editeur et l'execution : la requete est du C# natif.</summary>
    /// <param name="model">DbContext compile de la connexion, ou null : C# seul (fautes de syntaxe uniquement).</param>
    /// <param name="parameters">Parametres du dashboard : exposes au script (QueryGlobals), avec leurs valeurs ; l'editeur les donne sans valeur.</param>
    public static ICSharpLanguageService GetLanguageService(LinqModelAssembly? model, IReadOnlyList<LinqParameter>? parameters = null)
    {
        // Le source genere sert de cle : memes noms, types et valeurs, meme service. L'editeur donne des parametres sans
        // valeur (son service ne bouge pas) ; une execution a ses valeurs, d'ou la borne : trop de jeux, on repart de zero.
        var globals = LinqQuerySource.BuildGlobals(parameters ?? []);
        var services = model is null ? WithoutModel : ByModel.GetValue(model, static _ => new());
        if (services.Count >= MaxServicesPerModel && !services.ContainsKey(globals))
            services.Clear();

        return services.GetOrAdd(globals, static (source, state) => new Lazy<ICSharpLanguageService>(() => CreateLanguageService(state, source)), model).Value;
    }

    /// <summary>Compile et charge le modele genere ; leve <see cref="LinqCompilationException"/> en cas d'echec.</summary>
    public static async Task<LinqModelAssembly> CompileModelAsync(EfGeneratedModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Projet a part, sans editeur : le modele est compile seul, une fois par schema.
        using var service = new CSharpLanguageService($"EfModel_{model.ContextClassName}", () => new CSharpProjectOptions
        {
            References = LinqQuerySource.PlatformReferences,
            CompilationOptions = LinqQuerySource.ModelCompilationOptions,
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
    /// Compile une requete (C# natif, instructions de haut niveau) depuis le projet que l'editeur analyse ; les erreurs
    /// sont dans le resultat, pas levees. Les <paramref name="parameters"/> sont exposes au script par leur nom (QueryGlobals).
    /// </summary>
    /// <param name="log">Recoit les Log.* de la requete (runtime de script de Quill).</param>
    public static async Task<CSharpCompilationResult> CompileQueryAsync(LinqModelAssembly model, string query, IReadOnlyList<LinqParameter>? parameters = null, Action<LogLevel, string>? log = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(query);

        // Projet meme de l'editeur (references, contexte genere) : ce qui est souligne est ce qui echoue.
        ICSharpLanguageService service;
        try
        {
            service = GetLanguageService(model, parameters);
        }
        catch (ArgumentException ex)
        {
            // Valeur de parametre inconvertible en son type.
            throw new LinqCompilationException(ex.Message, []);
        }

        return await service.CompileAsync(query, new CSharpCompileOptions
        {
            AssemblyName = $"LinqQuery_{Guid.NewGuid():N}",

            // Le modele est servi tel quel (EF garde ses types en cache), le reste vient du runtime.
            ResolveAssembly = name => AssemblyName.ReferenceMatchesDefinition(name, model.Assembly.GetName()) ? model.Assembly : null,
            Log = log,
        }, cancellationToken);
    }

    /// <summary>
    /// Execute la requete (<see cref="CSharpCompilationResult.RunEntryPointAsync"/> de Quill) sur <paramref name="context"/> :
    /// seuls les Dump(valeur, titre), recus par <paramref name="dump"/>, exposent des donnees. Une exception du script est
    /// relevee telle quelle ; l'annulation rend la main tout de suite (<see cref="CSharpRunAbandonedException"/>), le script
    /// se terminant en arriere-plan.
    /// </summary>
    public static async Task RunAsync(LinqModelAssembly model, CSharpCompilationResult query, DbContext context, Action<object?, string?>? dump, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        // Dump (AsyncLocal, propre a ce flux) puis Db (propre a l'assembly de la requete) : poses avant le depart du script.
        model.DumpSinkProperty.SetValue(null, dump);
        query.Assembly!.GetType(LinqQuerySource.GlobalsClass)!
            .GetProperty(LinqQuerySource.ContextGlobal, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!
            .SetValue(null, context);

        await query.RunEntryPointAsync(cancellationToken: cancellationToken);
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
    private static CSharpLanguageService CreateLanguageService(LinqModelAssembly? model, string globals)
        => new($"Linq_{model?.Model.ContextClassName ?? "SansConnexion"}", () => new CSharpProjectOptions
        {
            References = model is null ? LinqQuerySource.PlatformReferences : [.. LinqQuerySource.PlatformReferences, model.Reference],
            CompilationOptions = LinqQuerySource.CompilationOptions,
            ParseOptions = LinqQuerySource.ParseOptions,
            DocumentName = "requete.cs",

            // Usings implicites et alias QueryDbContext : fixes, Roslyn ne les reanalyse pas a chaque frappe.
            // Dump vient du modele, Log et Script du runtime de Quill.
            // QueryGlobals expose le contexte (Db) et les parametres du dashboard : le script les utilise tels quels.
            AdditionalDocuments = new Dictionary<string, string>
            {
                [LinqQuerySource.GlobalUsings] = LinqQuerySource.BuildGlobalUsings(model?.Model),
                [LinqQuerySource.GlobalDocuments] = globals,
            },
            IncludeScriptRuntime = true,

            // Sans DbContext, les tables n'existent pas : seules les fautes de syntaxe ont un sens.
            IncludeSemanticDiagnostics = model is not null,
        });
}
