using Microsoft.EntityFrameworkCore;
using Querify.Languages.Linq.Modeling;
using Querify.Languages.Linq.Queries;
using Quill.Code.Analysis;
using Quill.Code.CSharp;
using Quill.Code.CSharp.Compilation;
using Quill.Code.Scripting;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Querify.Languages.Linq.Compilation;

/// <summary>
/// Requetes LINQ via Quill : DbContext genere (une fois par schema), QueryGlobals (une fois par modele et jeu de
/// parametres), services C# de l'editeur, puis chaque requete, compilee depuis le projet meme de l'editeur.
/// Sans etat propre hormis le cache des services.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "App non publiee en trimmed : compilation et reflexion assumees.")]
[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "App non publiee en AOT : compilation et reflexion assumees.")]
public static class LinqCompiler
{
    private const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // Un service par QueryGlobals compile (libere avec lui) : references, contexte et parametres ne changent pas d'une
    // frappe a l'autre ni d'une execution a l'autre (les valeurs sont posees a l'execution), Roslyn reutilise sa compilation.
    private static readonly ConditionalWeakTable<LinqParametersAssembly, CSharpLanguageService> Services = new();

    /// <summary>Service C# (Quill) partage par l'editeur et l'execution : la requete est du C# natif.</summary>
    /// <param name="parameters">QueryGlobals compile : DbContext de la connexion (ou C# seul) et parametres du dashboard.</param>
    public static CSharpLanguageService GetLanguageService(LinqParametersAssembly parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return Services.GetValue(parameters, CreateLanguageService);
    }

    /// <summary>Compile et charge le modele genere ; leve <see cref="LinqCompilationException"/> en cas d'echec.</summary>
    public static async Task<LinqGeneratedModelAssembly> CompileModelAsync(EfGeneratedModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        // Projet a part, sans editeur : le modele est compile seul, une fois par schema.
        var service = new CSharpLanguageService(() => new CSharpLanguageOptions
        {
            Name = $"EfModel_{model.ContextClassName}",
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

        return new LinqGeneratedModelAssembly
        {
            Model = model,
            Assembly = assembly,
            ContextType = contextType,
            DumpSinkProperty = extensionsType.GetProperty(EfModelGenerator.DumpSinkPropertyName, BindingFlags.NonPublic | BindingFlags.Static)!,
            Reference = compiled.Reference!
        };
    }

    /// <summary>
    /// Compile et charge QueryGlobals (contexte et parametres, sans valeur) contre <paramref name="model"/> ;
    /// leve <see cref="LinqCompilationException"/> en cas d'echec.
    /// </summary>
    /// <param name="model">DbContext compile de la connexion, ou null : Db est un DbContext (C# seul).</param>
    /// <param name="parameters">Parametres du dashboard ; seuls leurs noms et types comptent.</param>
    public static async Task<LinqParametersAssembly> CompileParametersAsync(LinqGeneratedModelAssembly? model, IReadOnlyList<LinqParameter> parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var source = LinqQuerySource.BuildGlobals(model?.Model, parameters);
        var service = new CSharpLanguageService(() => new CSharpLanguageOptions
        {
            Name = $"LinqParameters_{model?.Model.ContextClassName ?? "SansConnexion"}",
            References = model is null ? LinqQuerySource.PlatformReferences : [.. LinqQuerySource.PlatformReferences, model.Reference],
            CompilationOptions = LinqQuerySource.ModelCompilationOptions,
            ParseOptions = LinqQuerySource.ParseOptions,
        });

        // Non collectable, comme le modele : les requetes (collectables) la referencent.
        var compiled = await service.CompileAsync(source, new CSharpCompileOptions
        {
            AssemblyName = $"LinqParameters_{Guid.NewGuid():N}",
            Collectible = false,
            ResolveAssembly = name => ResolveModel(model, name),
        }, cancellationToken);

        if (compiled.Assembly is not { } assembly)
            throw new LinqCompilationException("Les parametres de la requete ne compilent pas.", compiled.Errors.Select(e => e.Describe(source)).ToArray());

        var globalsType = assembly.GetType(LinqQuerySource.GlobalsClass, throwOnError: true)!;

        return new LinqParametersAssembly
        {
            Model = model,
            Assembly = assembly,
            ContextProperty = globalsType.GetProperty(LinqQuerySource.ContextGlobal, StaticMembers)!,
            ParameterProperties = LinqQuerySource.GetGlobalParameters(parameters)
                .ToDictionary(p => p.Name, p => globalsType.GetProperty(p.Name, StaticMembers)!, StringComparer.Ordinal),
            Reference = compiled.Reference!,
        };
    }

    /// <summary>
    /// Compile une requete (C# natif, instructions de haut niveau) depuis le projet que l'editeur analyse ; les erreurs
    /// sont dans le resultat, pas levees. Les parametres de <paramref name="parameters"/> sont exposes au script par leur nom.
    /// </summary>
    /// <param name="log">Recoit les Log.* de la requete (runtime de script de Quill).</param>
    public static async Task<CSharpCompilationResult> CompileQueryAsync(LinqParametersAssembly parameters, string query, Action<LogLevel, string>? log = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(query);

        // Projet meme de l'editeur (references, contexte genere, parametres) : ce qui est souligne est ce qui echoue.
        return await GetLanguageService(parameters).CompileAsync(query, new CSharpCompileOptions
        {
            AssemblyName = $"LinqQuery_{Guid.NewGuid():N}",

            // Modele et parametres sont servis tels quels (EF garde ses types en cache), le reste vient du runtime.
            ResolveAssembly = name => AssemblyName.ReferenceMatchesDefinition(name, parameters.Assembly.GetName())
                ? parameters.Assembly
                : ResolveModel(parameters.Model, name),
            Log = log,
        }, cancellationToken);
    }

    /// <summary>
    /// Execute la requete (<see cref="CSharpCompilationResult.RunEntryPointAsync"/> de Quill) sur <paramref name="context"/> :
    /// seuls les Dump(valeur, titre), recus par <paramref name="dump"/>, exposent des donnees. Une valeur de parametre qui ne
    /// convient pas a son type leve <see cref="LinqCompilationException"/> avant le depart ; une exception du script est
    /// relevee telle quelle ; l'annulation rend la main tout de suite (<see cref="CSharpRunAbandonedException"/>), le script
    /// se terminant en arriere-plan.
    /// </summary>
    /// <param name="values">Valeurs des parametres, par nom ; un parametre absent vaut null.</param>
    public static async Task RunAsync(LinqParametersAssembly parameters, CSharpCompilationResult query, DbContext context, Action<object?, string?>? dump, IReadOnlyList<LinqParameter>? values = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        var model = parameters.Model
            ?? throw new InvalidOperationException("Une requete ne s'execute que sur le modele d'une connexion.");

        // Toutes les valeurs sont converties avant d'en poser une : une erreur n'en laisse aucune a moitie posee.
        Dictionary<PropertyInfo, object?> converted = [];
        try
        {
            foreach (var (name, parameter) in LinqQuerySource.GetGlobalParameters(values ?? []))
            {
                if (parameters.ParameterProperties.TryGetValue(name, out var property))
                    converted[property] = LinqQuerySource.ConvertValue(name, parameter);
            }
        }
        catch (ArgumentException ex)
        {
            throw new LinqCompilationException(ex.Message, []);
        }

        // Proprietes en AsyncLocal, posees dans ce flux avant le depart du script : Dump, Db puis les parametres.
        model.DumpSinkProperty.SetValue(null, dump);
        parameters.ContextProperty.SetValue(null, context);
        foreach (var (property, value) in converted)
            property.SetValue(null, value);

        await query.RunEntryPointAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Contexte EF du modele sur une chaine de connexion SQL Server.</summary>
    public static DbContext CreateContext(LinqGeneratedModelAssembly model, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(model.ContextType))!;
        builder.UseSqlServer(connectionString)
               // Lecture seule : pas de suivi des entites, inutile pour afficher des resultats.
               .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        return (DbContext)Activator.CreateInstance(model.ContextType, builder.Options)!;
    }

    private static Assembly? ResolveModel(LinqGeneratedModelAssembly? model, AssemblyName name)
        => model is not null && AssemblyName.ReferenceMatchesDefinition(name, model.Assembly.GetName()) ? model.Assembly : null;

    /// <summary>Projet cree par Quill a sa premiere utilisation : le declarer ne coute rien.</summary>
    private static CSharpLanguageService CreateLanguageService(LinqParametersAssembly parameters)
    {
        var model = parameters.Model;

        return new(() => new CSharpLanguageOptions
        {
            Name = $"Linq_{model?.Model.ContextClassName ?? "SansConnexion"}",
            References = model is null
                ? [.. LinqQuerySource.PlatformReferences, parameters.Reference]
                : [.. LinqQuerySource.PlatformReferences, model.Reference, parameters.Reference],
            CompilationOptions = LinqQuerySource.CompilationOptions,
            ParseOptions = LinqQuerySource.ParseOptions,
            DocumentName = "requete.cs",

            // Usings implicites, alias QueryDbContext et import de QueryGlobals : fixes, Roslyn ne les reanalyse pas a
            // chaque frappe. Dump vient du modele, Db et les parametres de QueryGlobals, Log et Script du runtime de Quill.
            AdditionalDocuments = new Dictionary<string, string>
            {
                [LinqQuerySource.GlobalUsings] = LinqQuerySource.BuildGlobalUsings(model?.Model),
            },
            IncludeScriptRuntime = true,

            // Sans DbContext, les tables n'existent pas : seules les fautes de syntaxe ont un sens.
            IncludeSemanticDiagnostics = model is not null,
        });
    }
}
